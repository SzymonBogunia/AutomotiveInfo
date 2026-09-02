# AutomotiveInfo

Serwis motoryzacyjny zbudowany na Umbraco 17 (.NET), z Delivery API, customowym API newsów, dashboardem redakcyjnym w backoffice i wyszukiwarką opartą o Examine.

## Wymagania wstępne

Zainstaluj i zweryfikuj **przed** klonowaniem repo:

| Narzędzie | Wersja | Weryfikacja |
|---|---|---|
| .NET SDK | 9.x | `dotnet --version` |
| Node.js | ≥ 22.15.0 (LTS) | `node -v` |
| npm | dołączony do Node | `npm -v` |
| SQL Server | LocalDB / Express / pełny | — |
| Git | dowolna aktualna | `git --version` |

**Windows — jeśli `npm` rzuca błąd o polityce wykonywania skryptów:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Zaufaj lokalnemu certyfikatowi HTTPS** (wymagane — projekt komunikuje się sam ze sobą po HTTPS dla proxy demo):
```powershell
dotnet dev-certs https --trust
```

---

## 1. Klonowanie i pierwsze uruchomienie

```powershell
https://github.com/SzymonBogunia/AutomotiveInfo/AutomotiveInfo.git
cd AutomotiveInfo
```

### 1.1 Baza danych

Utwórz pustą bazę SQL Server o nazwie `AutomotiveInfoDb` (lub zmień nazwę w connection stringu w kroku 1.2). Umbraco założy schemat automatycznie przy pierwszym starcie (`InstallUnattended: true`).

### 1.2 Sekrety (wymagane — aplikacja nie wystartuje bez nich)

Ze względów bezpieczeństwa `appsettings.json` **nie zawiera** prawdziwych haseł/kluczy — tylko placeholdery. Ustaw je lokalnie przez [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets):

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:umbracoDbDSN" "Server=localhost;Database=AutomotiveInfoDb;User Id=sa;Password=<TWOJE_HASLO_SQL>;TrustServerCertificate=True;"
dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserPassword" "<HASLO_ADMINA_BACKOFFICE>"
dotnet user-secrets set "Umbraco:CMS:DeliveryApi:ApiKey" "<TWOJ_KLUCZ_DELIVERY_API>"
dotnet user-secrets set "Umbraco:CMS:Imaging:HMACSecretKey" "<TWOJ_KLUCZ_HMAC>"
```

Wygeneruj bezpieczne wartości, np.:
```powershell
[guid]::NewGuid().ToString()                                          # klucz Delivery API
[Convert]::ToBase64String((New-Object byte[] 64 | %{[System.Security.Cryptography.RandomNumberGenerator]::Fill($_); $_}))  # HMAC
```

> Hasło unattended admina musi pasować do konta `<UnattendedUserEmail z appsettings.json>` — jeśli baza już istnieje z innym hasłem, zmień je w backoffice zamiast w configu.

### 1.3 Zbuduj i uruchom backend

```powershell
dotnet build
dotnet run
```

Aplikacja startuje na `https://localhost:44328`. Przy pierwszym uruchomieniu Umbraco automatycznie zaimportuje strukturę treści z uSync (`ImportAtStartup`).


**Cache manifestu:** backoffice cache'uje `umbraco-package.json` (~10s w trybie dev). Po zmianach w dashboardzie odśwież backoffice (F5).

### 1.4 Zbuduj arkusz stylów strony (Tailwind)

Style strony publicznej to osobny pakiet frontendowy budowany przez Vite — **nie jest częścią `dotnet build`**, tak samo jak dashboard w kroku 1.4.

```powershell
cd Client/Frontend
npm install
npm run build
cd ../..
```

To generuje `wwwroot/dist/site.css`, który `_Layout.cshtml` ładuje bezpośrednio (bez CDN). Bez tego kroku strona wystartuje, ale będzie bez stylów. Podczas developmentu użyj `npm run dev` zamiast `npm run build` — przebudowuje CSS przy każdym zapisie (bez live-reloadu w przeglądarce, trzeba odświeżyć ręcznie).

---

## 2. Logowanie do backoffice

```
URL:      https://localhost:44328/umbraco
Login:    <UnattendedUserEmail z appsettings.json, np. automotive@admin>
Hasło:    <to, które ustawiłeś w user-secrets, krok 1.2>
```

---

## 3. Funkcje i endpointy

### 3.1 Custom API newsów

```
GET /api/v1/news/latest?tag=<opcjonalny_tag>&count=<1-20, domyślnie 3>
```
Zwraca DTO artykułów (title, url, date, tag, imageUrl) z listy aktualności. `count` jest przycinane do zakresu 1–20.

### 3.2 Delivery API (headless)

```
GET https://localhost:44328/umbraco/delivery/api/v2/content
```
Wymaga nagłówka `Api-Key` (wartość z user-secrets, krok 1.2) — publiczny dostęp anonimowy jest **wyłączony** (`PublicAccess: false`). Dokumentacja: https://docs.umbraco.com/umbraco-cms/reference/content-delivery-api

Filtr po tagach wymaga jednorazowego rebuildu indeksu — patrz sekcja 3.6.

### 3.3 Swagger

```
https://localhost:44328/swagger
```
Wystawia `AutomotiveInfo News API` (custom kontrolery `[ApiController]`) oraz endpointy Umbraco Management API.

### 3.4 Headless demo

```
https://localhost:44328/headless-demo/index.html
```
Statyczna strona demonstrująca konsumpcję treści przez Delivery API. **Nie zawiera klucza API w kliencie** — requesty idą przez serwerowy proxy (`GET /api/demo/news`, `GET /api/demo/news/{id}`), który dokleja `Api-Key` po stronie backendu. Klucz nigdy nie trafia do przeglądarki.

### 3.5 Wyszukiwarka

```
/pl/strona-wyszukiwania/ (lub odpowiednik /en/search-page/)
```
Oparta o Examine, z boostem trafności (tytuł ×3, tag ×2.5, nazwa węzła ×2). Wpisanie frazy znajduje też artykuły otagowane tą frazą (bez osobnej składni).

### 3.6 Rebuild indeksu Examine / Delivery API

**Wymagany po każdym czystym starcie z pustą bazą** oraz po większych zmianach w treści tagów — filtr `tag:` w Delivery API i część wyników wyszukiwarki nie zadziałają bez tego kroku:

1. Backoffice → **Settings** → **Examine Management**
2. Znajdź `DeliveryApiContentIndex`
3. Kliknij **Rebuild Index**

---

## 4. Kultury / języki

| Kultura | Domena | Status |
|---|---|---|
| `pl-PL` | domyślna | ✅ pełna treść |
| `en-US` | `/en/` | ✅ pełna treść |

Wszystkie stringi UI (nawigacja, etykiety) pochodzą z **Dictionary** (Settings → Dictionary) — 16 kluczy × 2 kultury. Nie hardkoduj tekstu w widokach.

---

## 5. Struktura repo (skrót)

```
AutomotiveInfo/
├── wwwroot/
│   └── dist/
│       └── site.css              ← build output stylów (generowany, patrz krok 1.5)
├── Controllers/              ← customowe kontrolery API (NewsApiController, DemoProxyController, SearchPageController)
├── Composers/                ← rejestracje DI per zagadnienie (IComposer, auto-discoverowane)
├── Models/                   ← DTO (NewsArticleDto itd.)
├── Views/                    ← Razor views
├── App_Plugins/
│   └── EditorialStatsDashboard/
│       └── dist/             ← build output dashboardu (generowany, patrz krok 1.4)
├── Client/
│   └── EditorialStatsDashboard/   ← źródła TS/Lit dashboardu, build przez Vite
│   └── Frontend/                  ← źródła Tailwind CSS strony publicznej, build przez Vite
├── uSync/                    ← wyeksportowany schemat treści (auto-import przy starcie)
└── appsettings.json           ← config z placeholderami; prawdziwe wartości w user-secrets
```

---

## 6. Rozwiązywanie problemów

| Objaw | Przyczyna | Rozwiązanie |
|---|---|---|
| `npm` — błąd polityki wykonywania skryptów | PowerShell blokuje `.ps1` | `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser` |
| `The remote certificate is invalid: UntrustedRoot` przy wywołaniach self-call (demo proxy) | Dev-cert niezaufany server-to-server | `dotnet dev-certs https --clean` → `dotnet dev-certs https --trust` → restart `dotnet run` |
| Filtr `tag:` w Delivery API nic nie zwraca | Indeks nie zbudowany | Patrz sekcja 3.6 |
| Import uSync rzuca `Duplicate: Item key…` | Duplikaty w `uSync/v17/Domains/` | Usuń pliki-duplikaty (ten sam `ContentKey`, różne nazwy plików), potem Import ponownie |
| 401 na demo/Delivery API | Nieaktualny/nieprawidłowy klucz w user-secrets | Sprawdź `dotnet user-secrets list`, upewnij się że backend i demo proxy używają tego samego klucza |