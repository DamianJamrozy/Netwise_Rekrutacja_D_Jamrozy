# Opis projektu / Project Description

## PL

### Nazwa projektu
**Netwise_Rekrutacja_D_Jamrozy**

### Krótki opis
Projekt został przygotowany jako aplikacja webowa w technologii **ASP.NET Core MVC** z wykorzystaniem **Dependency Injection** oraz podziału na warstwy odpowiedzialności. Głównym założeniem było zbudowanie prostego, ale porządnie zaprojektowanego panelu, który komunikuje się z zewnętrznym API oraz zapisuje dane lokalnie do pliku tekstowego.
Oprogramowanie zostało rozwinięte o panel administratora, które pozwala nimi zarządzać z poziomu interfejsu weebowego.

Aplikacja pobiera ciekawostki z endpointu:

`https://catfact.ninja/fact`

Każda odpowiedź z API jest zapisywana lokalnie w pliku `catfacts.txt` w osobnej linii, w kontrolowanym formacie, który pozwala później wygodnie odczytywać dane, sortować je i wykonywać operacje CRUD.

### Zakres funkcjonalny
Projekt składa się z dwóch głównych części:

#### 1. Widok publiczny
Strona startowa pobiera nową ciekawostkę z zewnętrznego API i prezentuje ją użytkownikowi w czytelnej formie jako bieżący wpis. Przy każdym takim pobraniu rekord jest automatycznie dopisywany do pliku danych.

#### 2. Panel administracyjny
Panel `/admin` umożliwia:
- podgląd zapisanych wpisów z pliku,
- pobranie nowego wpisu z API bez przeładowania całej strony,
- ręczne dodanie wpisu,
- edycję istniejącego wpisu,
- usunięcie wybranego wpisu,
- sortowanie danych po kolumnach,
- podgląd logów aplikacyjnych i logów operacji CRUD,
- stronicowanie logów, aby nie wczytywać zbyt dużej liczby rekordów jednocześnie.

### Obsługa plików
Aplikacja nie zakłada, że pliki istnieją już w momencie startu. Przy uruchomieniu sprawdzany jest katalog `Data`, a jeśli go brakuje, zostaje utworzony automatycznie. To samo dotyczy plików:
- `catfacts.txt`
- `CRUD.log.txt`
- `log.txt`

Dzięki temu aplikacja nie kończy działania błędem tylko dlatego, że środowisko startowe nie zostało wcześniej przygotowane ręcznie.

### Logowanie i historia zmian
W projekcie zostały rozdzielone dwa typy logów:

#### `log.txt`
Zawiera błędy aplikacji, kody błędów, czas wystąpienia, ścieżkę żądania i szczegóły techniczne potrzebne do diagnozy problemu.

#### `CRUD.log.txt`
Zawiera historię operacji wykonywanych na danych, w tym:
- datę i godzinę,
- adres IP klienta,
- typ operacji,
- numer linii w pliku,
- identyfikator wpisu,
- zwięzły opis zmiany.

Dodatkowo logi są utrzymywane z ostatnich 30 dni. Starsze wpisy są automatycznie usuwane podczas pracy aplikacji, żeby pliki nie rosły bez końca.

### Bezpieczeństwo
Projekt został przygotowany z naciskiem na bezpieczne podejście do operacji wejścia/wyjścia i komunikacji webowej. Zastosowano między innymi:
- Dependency Injection,
- walidację danych wejściowych,
- ochronę anty-CSRF dla operacji modyfikujących dane,
- kontrolę dozwolonych nazw i rozszerzeń plików,
- zabezpieczenie przed odczytem ścieżek spoza katalogu aplikacji,
- oddzielenie logiki aplikacyjnej od kontrolerów,
- nagłówki bezpieczeństwa HTTP,
- kontrolowany sposób obsługi wyjątków i logowania błędów.

### Struktura techniczna
Projekt został podzielony na osobne obszary odpowiedzialności:
- `Controllers` – obsługa żądań HTTP,
- `Services` – logika aplikacyjna i operacje na danych,
- `Models` – modele domenowe, ViewModel-e i konfiguracja,
- `Infrastructure` – elementy techniczne, takie jak middleware czy serializacja,
- `Views` – interfejs użytkownika oparty o Razor,
- `wwwroot` – zasoby statyczne, CSS i JavaScript,
- `Data` – pliki robocze aplikacji.

### Cel projektu
Celem projektu nie było tylko „wyświetlenie danych z API”, ale przygotowanie małej aplikacji, która wygląda spójnie, ma sensowną architekturę i daje się dalej rozwijać bez przepisywania wszystkiego od nowa. Z tego powodu od początku został zachowany podział na warstwy, interfejsy, obsługę błędów, logowanie i zabezpieczenia.

---

## ENG

### Project name
**Netwise_Rekrutacja_D_Jamrozy**

### Short description
The project was developed as a web application using ASP.NET Core MVC, with Dependency Injection and a layered architecture separating responsibilities. The main goal was to build a simple yet well-structured panel that communicates with an external API and stores data locally in a text file.
The software was extended with an administrator panel that allows managing the data through a web interface.

The application retrieves cat facts from the following endpoint:

`https://catfact.ninja/fact`

Every API response is stored locally in `catfacts.txt`, one entry per line, using a controlled format that makes reading, sorting and CRUD operations straightforward.

### Functional scope
The project consists of two main parts:

#### 1. Public view
The home page fetches a new fact from the external API and displays it as the current featured entry. Every successful fetch is automatically appended to the local data file.

#### 2. Admin panel
The `/admin` panel allows the user to:
- preview stored entries from the file,
- fetch a new fact from the API without reloading the whole page,
- add a manual entry,
- edit an existing entry,
- delete a selected entry,
- sort table data by columns,
- preview application logs and CRUD audit logs,
- paginate log data to avoid loading too many records at once.

### File handling
The application does not assume that required files already exist. On startup, it checks the `Data` directory and creates it automatically if needed. The same applies to these files:
- `catfacts.txt`
- `CRUD.log.txt`
- `log.txt`

Because of that, the application does not fail just because the environment was not prepared manually beforehand.

### Logging and change history
Two separate log types are used in the project:

#### `log.txt`
Contains application errors, error codes, timestamps, request paths and technical details needed for troubleshooting.

#### `CRUD.log.txt`
Contains a history of data operations, including:
- date and time,
- client IP address,
- operation type,
- line number in the file,
- entry identifier,
- a short description of what was changed.

In addition, logs are retained for the last 30 days only. Older entries are removed automatically during application work so the files do not grow indefinitely.

### Security
The project was designed with a strong focus on safe file handling and secure web communication. The implementation includes, among other things:
- Dependency Injection,
- input validation,
- anti-CSRF protection for state-changing operations,
- validation of allowed file names and extensions,
- protection against reading paths outside the application data directory,
- separation of business logic from controllers,
- HTTP security headers,
- controlled exception handling and structured error logging.

### Technical structure
The solution is split into separate responsibility areas:
- `Controllers` – HTTP request handling,
- `Services` – application logic and data operations,
- `Models` – domain models, view models and configuration,
- `Infrastructure` – technical components such as middleware and serialization,
- `Views` – Razor-based UI,
- `wwwroot` – static assets, CSS and JavaScript,
- `Data` – application working files.

### Project goal
The purpose of this project was not only to “display data from an API”, but to build a small application with a clean structure, consistent behavior and room for further development without rewriting the whole solution. That is why the architecture, interfaces, error handling, logging and security concerns were treated as part of the foundation from the very beginning.
