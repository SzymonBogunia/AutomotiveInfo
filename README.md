# AutomotiveInfo - Portal Motoryzacyjny

Portal motoryzacyjny pełniący rolę bazy wiedzy dla kierowców i pasjonatów aut. Projekt oparty jest o nowoczesny system zarządzania treścią **Umbraco CMS 17** oraz platformę **.NET 10**.

---

## Wymagania środowiskowe

Przed uruchomieniem projektu upewnij się, że na Twoim komputerze zainstalowane są następujące elementy:
* **.NET 10 SDK** (lub nowszy kompatybilny)
* **Microsoft SQL Server** 
* **SQL Server Management Studio (SSMS)** lub inne narzędzie do zarządzania bazą danych

---

## Konfiguracja i automatyczna instalacja (Unattended Install)

Projekt wykorzystuje mechanizm **Unattended Install** wbudowany w Umbraco. Oznacza to, że baza danych oraz konto administratora są tworzone automatycznie podczas pierwszego uruchomienia aplikacji – **bez konieczności przechodzenia przez ręczny kreator w przeglądarce**.

### 1. Przygotowanie bazy danych
Otwórz SQL Server Management Studio i stwórz nową, całkowicie pustą bazę danych o nazwie: AutomotiveInfoDb (CREATE DATABASE AutomotiveInfoDb;)
### 2. Plik konfiguracyjny appsettings.json
Upewnij się, że w pliku appsettings.json na poziomie katalogu głównego projektu znajduje się poprawna konfiguracja połączenia oraz dane administratora (szczególnie właściwy Connection String dla Twojego lokalnego serwera SQL). Dane administratora są automatycznie wstrzykiwane do tabeli umbracoUser przy pierwszym starcie projektu.

---

## Instrukcja uruchomienia
Aby zbudować i uruchomić projekt lokalnie, wykonaj poniższe komendy w głównym katalogu projektu:

Przywrócenie pakietów i budowanie aplikacji:
dotnet build

Uruchomienie projektu:
dotnet run

Po pomyślnym uruchomieniu aplikacja rozpocznie nasłuchiwanie na portach deweloperskich. Przejdź w przeglądarce (zalecany tryb incognito przy pierwszym logowaniu) pod adres podany w konsoli, dodając na końcu ścieżkę do panelu administratora:
https://localhost:44309/umbraco

Zaloguj się do panelu administracyjnego (Backoffice), używając skonfigurowanych danych:

E-mail: automotive@admin

Hasło: ciezkiehaslo

---

### Wersjonowanie struktury (uSync)
Wszystkie zmiany w strukturze danych (Typy Dokumentów, Typy Danych, Szablony) są automatycznie rejestrowane i zapisywane do plików tekstowych w folderze /uSync/ na poziomie głównym projektu.

Dzięki temu konfiguracja bazy danych jest w pełni śledzona przez system kontroli wersji Git. Po sklonowaniu repozytorium na inne środowisko, uruchomienie polecenia dotnet run automatycznie zaimportuje całą strukturę i odtworzy panel CMS w identycznym stanie.
   
