using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Netwise_Rekrutacja_D_Jamrozy.Models.Domain;
using Netwise_Rekrutacja_D_Jamrozy.Models.Options;
using Netwise_Rekrutacja_D_Jamrozy.Services.Interfaces;

namespace Netwise_Rekrutacja_D_Jamrozy.Services;

public sealed class CatFactService : ICatFactService
{
    private readonly HttpClient httpClient;
    private readonly StorageOptions storageOptions;

    public CatFactService(HttpClient httpClient, IOptions<StorageOptions> storageOptions)
    {
        this.httpClient = httpClient;
        this.storageOptions = storageOptions.Value;
    }

    public async Task<CatFactEntry> GetNewFactAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(string.Empty, cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException(
                $"External API returned unexpected status code: {(int)response.StatusCode} {response.StatusCode}.",
                null,
                response.StatusCode);
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            throw new InvalidOperationException("External API returned an empty response.");
        }

        CatFactApiResponse? apiResponse;

        try
        {
            apiResponse = JsonSerializer.Deserialize<CatFactApiResponse>(
                responseContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("External API returned invalid JSON.", exception);
        }

        if (apiResponse is null)
        {
            throw new InvalidOperationException("External API response could not be deserialized.");
        }

        if (string.IsNullOrWhiteSpace(apiResponse.Fact))
        {
            throw new InvalidOperationException("External API returned an empty fact.");
        }

        var fact = apiResponse.Fact.Trim();

        if (fact.Length > storageOptions.MaxFactLength)
        {
            throw new InvalidOperationException(
                $"External API returned a fact longer than allowed limit of {storageOptions.MaxFactLength} characters.");
        }

        var normalizedLength = apiResponse.Length > 0 ? apiResponse.Length : fact.Length;

        return new CatFactEntry
        {
            Id = Guid.NewGuid(),
            Fact = fact,
            Length = normalizedLength,
            CreatedAtUtc = DateTime.UtcNow,
            Source = "api"
        };
    }
}