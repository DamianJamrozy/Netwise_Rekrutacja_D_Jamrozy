(() => {
    const configElement = document.getElementById("admin-page-config");

    if (!configElement) {
        return;
    }

    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
    const selectedFileLabel = document.getElementById("selected-file-label");
    const entriesCount = document.getElementById("entries-count");
    const entriesTableBody = document.getElementById("entries-table-body");
    const entriesEmptyState = document.getElementById("entries-empty-state");
    const entriesTableWrap = document.getElementById("entries-table-wrap");
    const logsTableBody = document.getElementById("logs-table-body");
    const statusBox = document.getElementById("admin-status");
    const fileSelect = document.getElementById("fileName");
    const fetchFactButton = document.getElementById("fetch-fact-button");

    const entryFormTitle = document.getElementById("entry-form-title");
    const entryIdInput = document.getElementById("entry-id");
    const entryFactInput = document.getElementById("entry-fact");
    const createEntryButton = document.getElementById("create-entry-button");
    const updateEntryButton = document.getElementById("update-entry-button");
    const cancelEditButton = document.getElementById("cancel-edit-button");

    const factsFileName = configElement.dataset.factsFileName ?? "";
    const canManageFactsFile = configElement.dataset.canManageFactsFile === "true";
    let selectedFileName = configElement.dataset.selectedFileName ?? "";
    let currentEntries = readEntriesFromDom();
    let currentLogs = readLogsFromDom();

    const sortState = {
        entries: {
            key: "createdAtUtc",
            direction: "desc"
        },
        logs: {
            key: "lineNumber",
            direction: "desc"
        }
    };

    initializeSortButtons();

    if (fileSelect) {
        fileSelect.addEventListener("change", () => {
            selectedFileName = fileSelect.value;
        });
    }

    if (fetchFactButton) {
        fetchFactButton.addEventListener("click", async () => {
            await fetchNewFactAsync();
        });
    }

    if (createEntryButton) {
        createEntryButton.addEventListener("click", async () => {
            await createEntryAsync();
        });
    }

    if (updateEntryButton) {
        updateEntryButton.addEventListener("click", async () => {
            await updateEntryAsync();
        });
    }

    if (cancelEditButton) {
        cancelEditButton.addEventListener("click", () => {
            resetEntryForm();
        });
    }

    if (entriesTableBody && canManageFactsFile) {
        entriesTableBody.addEventListener("click", async event => {
            const actionButton = event.target.closest("[data-action]");

            if (!actionButton) {
                return;
            }

            const action = actionButton.getAttribute("data-action");
            const entryId = actionButton.getAttribute("data-entry-id");

            if (!entryId) {
                return;
            }

            if (action === "edit") {
                beginEdit(entryId);
                return;
            }

            if (action === "delete") {
                await deleteEntryAsync(entryId);
            }
        });
    }

    applyEntriesSort();
    applyLogsSort();
    refreshSortIndicators();

    function initializeSortButtons() {
        const sortButtons = document.querySelectorAll(".sort-button");

        sortButtons.forEach(button => {
            button.addEventListener("click", () => {
                const target = button.dataset.sortTarget;
                const key = button.dataset.sortKey;

                if (!target || !key || !sortState[target]) {
                    return;
                }

                if (sortState[target].key === key) {
                    sortState[target].direction = sortState[target].direction === "asc" ? "desc" : "asc";
                } else {
                    sortState[target].key = key;
                    sortState[target].direction = key === "createdAtUtc" || key === "lineNumber" ? "desc" : "asc";
                }

                if (target === "entries") {
                    applyEntriesSort();
                }

                if (target === "logs") {
                    applyLogsSort();
                }

                refreshSortIndicators();
            });
        });
    }

    function readEntriesFromDom() {
        if (!entriesTableBody) {
            return [];
        }

        const rows = entriesTableBody.querySelectorAll("tr[data-entry-id]");

        return Array.from(rows).map(row => ({
            id: row.dataset.entryId ?? "",
            lineNumber: Number(row.dataset.lineNumber ?? "0"),
            fact: decodeBase64Utf8(row.dataset.factBase64 ?? ""),
            createdAtUtc: row.dataset.createdAtUtc ?? "",
            length: Number(row.dataset.length ?? "0"),
            source: row.dataset.source ?? ""
        }));
    }

    function readLogsFromDom() {
        if (!logsTableBody) {
            return [];
        }

        const rows = logsTableBody.querySelectorAll("tr[data-line-number]");

        return Array.from(rows).map(row => ({
            lineNumber: Number(row.dataset.lineNumber ?? "0"),
            content: row.querySelector(".log-line-content-cell")?.textContent ?? ""
        }));
    }

    async function fetchNewFactAsync() {
        setButtonLoadingState(fetchFactButton, true, "Fetching...");
        showStatus("Loading a new cat fact...", "info");

        try {
            const response = await postJsonAsync("/api/facts/fetch", {});
            if (!response.success) {
                return;
            }

            showStatus(response.payload.message || "A new cat fact has been stored.", "success");
            await reloadEntriesAsync(factsFileName);
            resetEntryForm();
        } finally {
            setButtonLoadingState(fetchFactButton, false, "Fetch new fact");
        }
    }

    async function createEntryAsync() {
        if (!entryFactInput) {
            return;
        }

        const fact = entryFactInput.value.trim();

        if (!fact) {
            showStatus("Fact is required.", "error");
            entryFactInput.focus();
            return;
        }

        setButtonLoadingState(createEntryButton, true, "Saving...");

        try {
            const response = await postJsonAsync("/api/entries/create", { fact });

            if (!response.success) {
                return;
            }

            showStatus(response.payload.message || "A new entry has been created.", "success");
            await reloadEntriesAsync(factsFileName);
            resetEntryForm();
        } finally {
            setButtonLoadingState(createEntryButton, false, "Save manual entry");
        }
    }

    async function updateEntryAsync() {
        if (!entryIdInput || !entryFactInput) {
            return;
        }

        const id = entryIdInput.value.trim();
        const fact = entryFactInput.value.trim();

        if (!id) {
            showStatus("No entry is currently selected for editing.", "error");
            return;
        }

        if (!fact) {
            showStatus("Fact is required.", "error");
            entryFactInput.focus();
            return;
        }

        setButtonLoadingState(updateEntryButton, true, "Updating...");

        try {
            const response = await postJsonAsync("/api/entries/update", { id, fact });

            if (!response.success) {
                return;
            }

            showStatus(response.payload.message || "The entry has been updated.", "success");
            await reloadEntriesAsync(factsFileName);
            resetEntryForm();
        } finally {
            setButtonLoadingState(updateEntryButton, false, "Update selected entry");
        }
    }

    async function deleteEntryAsync(entryId) {
        const entry = currentEntries.find(value => value.id === entryId);
        const preview = entry?.fact ? buildFactPreview(entry.fact) : "this entry";
        const isConfirmed = window.confirm(`Delete ${preview}?`);

        if (!isConfirmed) {
            return;
        }

        showStatus("Deleting the selected entry...", "info");

        const response = await postJsonAsync("/api/entries/delete", { id: entryId });

        if (!response.success) {
            return;
        }

        showStatus(response.payload.message || "The entry has been deleted.", "success");
        await reloadEntriesAsync(factsFileName);

        if (entryIdInput?.value === entryId) {
            resetEntryForm();
        }
    }

    async function reloadEntriesAsync(fileName) {
        try {
            const response = await fetch(`/api/entries?fileName=${encodeURIComponent(fileName)}`, {
                method: "GET",
                headers: {
                    "Accept": "application/json"
                }
            });

            const payload = await tryReadJsonAsync(response);

            if (!response.ok || !payload?.success) {
                const message = payload?.message || "Failed to reload entries.";
                const errorCode = payload?.errorCode ? ` (${payload.errorCode})` : "";
                showStatus(`${message}${errorCode}`, "error");
                return;
            }

            currentEntries = payload.entries || [];
            selectedFileName = payload.selectedFileName || fileName;

            if (selectedFileLabel) {
                selectedFileLabel.textContent = selectedFileName;
            }

            applyEntriesSort();
            refreshSortIndicators();
        } catch {
            showStatus("A network or server error occurred while reloading the table.", "error");
        }
    }

    function applyEntriesSort() {
        if (!entriesTableBody) {
            return;
        }

        currentEntries = [...currentEntries].sort(compareEntries);
        renderEntries(currentEntries);
    }

    function applyLogsSort() {
        if (!logsTableBody) {
            return;
        }

        currentLogs = [...currentLogs].sort(compareLogs);
        renderLogs(currentLogs);
    }

    function compareEntries(left, right) {
        const key = sortState.entries.key;
        const direction = sortState.entries.direction === "asc" ? 1 : -1;

        let result = 0;

        switch (key) {
            case "lineNumber":
                result = left.lineNumber - right.lineNumber;
                break;
            case "createdAtUtc":
                result = new Date(left.createdAtUtc).getTime() - new Date(right.createdAtUtc).getTime();
                break;
            case "fact":
                result = left.fact.localeCompare(right.fact, undefined, { sensitivity: "base" });
                break;
            case "length":
                result = left.length - right.length;
                break;
            case "source":
                result = left.source.localeCompare(right.source, undefined, { sensitivity: "base" });
                break;
            case "id":
                result = left.id.localeCompare(right.id, undefined, { sensitivity: "base" });
                break;
            default:
                result = 0;
                break;
        }

        if (result === 0) {
            result = new Date(left.createdAtUtc).getTime() - new Date(right.createdAtUtc).getTime();
        }

        return result * direction;
    }

    function compareLogs(left, right) {
        const direction = sortState.logs.direction === "asc" ? 1 : -1;
        return (left.lineNumber - right.lineNumber) * direction;
    }

    function renderEntries(entries) {
        if (!entriesTableBody || !entriesCount || !entriesEmptyState || !entriesTableWrap) {
            return;
        }

        entriesTableBody.innerHTML = "";

        if (!entries.length) {
            entriesCount.textContent = "0";
            entriesEmptyState.classList.remove("hidden");
            entriesTableWrap.classList.add("hidden");
            return;
        }

        const rowsHtml = entries.map(entry => {
            const lineNumber = escapeHtml(String(entry.lineNumber));
            const createdAtUtc = escapeHtml(formatUtcDate(entry.createdAtUtc));
            const fact = escapeHtml(entry.fact);
            const length = escapeHtml(String(entry.length));
            const source = escapeHtml(entry.source);
            const id = escapeHtml(entry.id);

            const actionsHtml = canManageFactsFile
                ? `
                    <td class="actions-cell">
                        <div class="actions-stack">
                            <button type="button" class="table-action-button" data-action="edit" data-entry-id="${id}">
                                Edit
                            </button>
                            <button type="button" class="table-action-button table-action-button-danger" data-action="delete" data-entry-id="${id}">
                                Delete
                            </button>
                        </div>
                    </td>
                `
                : "";

            return `
                <tr data-entry-id="${id}" data-line-number="${lineNumber}">
                    <td>${lineNumber}</td>
                    <td>${createdAtUtc}</td>
                    <td>${fact}</td>
                    <td>${length}</td>
                    <td>${source}</td>
                    <td class="id-cell">${id}</td>
                    ${actionsHtml}
                </tr>
            `;
        }).join("");

        entriesTableBody.innerHTML = rowsHtml;
        entriesCount.textContent = String(entries.length);
        entriesEmptyState.classList.add("hidden");
        entriesTableWrap.classList.remove("hidden");
    }

    function renderLogs(logs) {
        if (!logsTableBody) {
            return;
        }

        logsTableBody.innerHTML = logs.map(log => `
            <tr data-line-number="${escapeHtml(String(log.lineNumber))}">
                <td class="log-line-number-cell">${escapeHtml(String(log.lineNumber))}</td>
                <td class="log-line-content-cell">${escapeHtml(log.content)}</td>
            </tr>
        `).join("");
    }

    function beginEdit(entryId) {
        if (!canManageFactsFile || !entryIdInput || !entryFactInput || !entryFormTitle) {
            return;
        }

        const entry = currentEntries.find(value => value.id === entryId);

        if (!entry) {
            showStatus("The selected entry could not be found in the current table.", "error");
            return;
        }

        entryIdInput.value = entry.id;
        entryFactInput.value = entry.fact;
        entryFactInput.focus();
        entryFormTitle.textContent = `Edit entry from line ${entry.lineNumber}`;

        createEntryButton?.classList.add("hidden");
        updateEntryButton?.classList.remove("hidden");
        cancelEditButton?.classList.remove("hidden");

        showStatus("Edit mode enabled for the selected entry.", "info");
    }

    function resetEntryForm() {
        if (!canManageFactsFile || !entryIdInput || !entryFactInput || !entryFormTitle) {
            return;
        }

        entryIdInput.value = "";
        entryFactInput.value = "";
        entryFormTitle.textContent = "Create manual entry";

        createEntryButton?.classList.remove("hidden");
        updateEntryButton?.classList.add("hidden");
        cancelEditButton?.classList.add("hidden");
    }

    async function postJsonAsync(url, payload) {
        try {
            const response = await fetch(url, {
                method: "POST",
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/json",
                    "RequestVerificationToken": antiForgeryToken,
                    "X-CSRF-TOKEN": antiForgeryToken
                },
                body: JSON.stringify(payload)
            });

            const responsePayload = await tryReadJsonAsync(response);

            if (!response.ok || !responsePayload?.success) {
                const message = responsePayload?.message || "The request failed.";
                const errorCode = responsePayload?.errorCode ? ` (${responsePayload.errorCode})` : "";
                showStatus(`${message}${errorCode}`, "error");

                return {
                    success: false,
                    payload: responsePayload
                };
            }

            return {
                success: true,
                payload: responsePayload
            };
        } catch {
            showStatus("A network or server error occurred while sending the request.", "error");

            return {
                success: false,
                payload: null
            };
        }
    }

    async function tryReadJsonAsync(response) {
        try {
            return await response.json();
        } catch {
            return null;
        }
    }

    function setButtonLoadingState(button, isLoading, loadingText) {
        if (!button) {
            return;
        }

        if (!button.dataset.defaultText) {
            button.dataset.defaultText = button.textContent || "";
        }

        button.disabled = isLoading;
        button.textContent = isLoading ? loadingText : button.dataset.defaultText;
    }

    function showStatus(message, type) {
        if (!statusBox) {
            return;
        }

        statusBox.textContent = message;
        statusBox.classList.remove("hidden", "status-box-error", "status-box-success", "status-box-info");

        if (type === "error") {
            statusBox.classList.add("status-box-error");
            return;
        }

        if (type === "success") {
            statusBox.classList.add("status-box-success");
            return;
        }

        statusBox.classList.add("status-box-info");
    }

    function refreshSortIndicators() {
        const sortButtons = document.querySelectorAll(".sort-button");

        sortButtons.forEach(button => {
            const target = button.dataset.sortTarget;
            const key = button.dataset.sortKey;

            button.classList.remove("sort-button-active", "sort-button-asc", "sort-button-desc");
            button.removeAttribute("aria-sort");

            if (!target || !key || !sortState[target]) {
                return;
            }

            if (sortState[target].key !== key) {
                return;
            }

            button.classList.add("sort-button-active");
            button.classList.add(sortState[target].direction === "asc" ? "sort-button-asc" : "sort-button-desc");
            button.setAttribute("aria-sort", sortState[target].direction === "asc" ? "ascending" : "descending");
        });
    }

    function formatUtcDate(value) {
        if (!value) {
            return "";
        }

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            return String(value);
        }

        const year = date.getUTCFullYear();
        const month = String(date.getUTCMonth() + 1).padStart(2, "0");
        const day = String(date.getUTCDate()).padStart(2, "0");
        const hours = String(date.getUTCHours()).padStart(2, "0");
        const minutes = String(date.getUTCMinutes()).padStart(2, "0");
        const seconds = String(date.getUTCSeconds()).padStart(2, "0");

        return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
    }

    function escapeHtml(value) {
        return value
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;")
            .replaceAll("'", "&#39;");
    }

    function buildFactPreview(value) {
        if (!value) {
            return "this entry";
        }

        return value.length <= 80
            ? `"${value}"`
            : `"${value.slice(0, 80)}..."`;
    }

    function decodeBase64Utf8(value) {
        if (!value) {
            return "";
        }

        try {
            const binaryString = window.atob(value);
            const bytes = Uint8Array.from(binaryString, character => character.charCodeAt(0));
            return new TextDecoder().decode(bytes);
        } catch {
            return "";
        }
    }
})();