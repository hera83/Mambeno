# Mambeno API

Et selvhostet REST API, der holder en lokal kopi af alle opskrifter og kategorier fra [mambeno.dk](https://mambeno.dk) og stiller dem til rådighed for dine egne apps, scripts og automatiseringer.

Kopien er kun tekst (ingen billeder) og opdateres automatisk hver nat. Svarene har samme form som "Mors Opskrifter"-API'et, så en klient kan flette de to kilder sammen.

> [!IMPORTANT]
> **Betalt Mambeno-medlemskab er et krav.**
> Opskrifterne er Mambenos arbejde, og nogen skal betale for at de bliver skrevet, testet og vedligeholdt. Hvis du bruger denne løsning, **skal du selv have et aktivt betalt medlemskab på [mambeno.dk](https://mambeno.dk)** så længe du bruger den. Det er en betingelse i [licensen](LICENSE.txt), ikke bare en opfordring.
>
> Løsningen er kun et **supplement** til dit eget medlemskab, ikke en erstatning for det.

> [!WARNING]
> **Uofficiel løsning, på eget ansvar.**
> Projektet er ikke registreret hos, godkendt af eller tilknyttet Mambeno på nogen måde. Du installerer og bruger det helt på eget ansvar. Læs [Ansvar og forbehold](#ansvar-og-forbehold), før du går i gang.

## Hvad kan den?

- **Natlig synkronisering** af alle opskrifter, ingredienser, fremgangsmåder og kategorier via Mambenos offentlige WordPress-API. Kun nye og ændrede opskrifter hentes, og forespørgslerne sendes én ad gangen med pause imellem, så Mambenos server ikke belastes.
- **Sikker sletning**: en opskrift slettes kun lokalt, når Mambeno selv bekræfter at den er væk. Ved fejl eller ufuldstændige svar slettes intet.
- **Søgning og filtrering** på tekst, kategori, ingredienser, samlet tid og ændringsdato, med sortering og sideinddeling.
- **API-nøgler** (`x-api-key`) på alle endpoints. Med en master-nøgle opretter du almindelige nøgler til dine klienter.
- **Swagger UI** til at afprøve API'et direkte i browseren.
- **Health-endpoint**, der fortæller hvornår data sidst blev opdateret.

## Endpoints

| Metode | Sti | Adgang | Beskrivelse |
|---|---|---|---|
| `GET` | `/api/v1/recipes` | API-nøgle | Søg og filtrér opskrifter |
| `GET` | `/api/v1/recipes/{id}` | API-nøgle | Én opskrift med ingredienser og fremgangsmåde |
| `GET` | `/api/v1/categories` | API-nøgle | Alle kategorier med antal opskrifter |
| `GET` | `/api/v1/categories/{id}` | API-nøgle | Én kategori |
| `GET` | `/health` | Åben | Status og tidspunkt for seneste synkronisering |
| | `/keys/...` | Master-nøgle | Opret, ret, forny og slet API-nøgler |
| `GET` | `/log` | Master-nøgle | Søg i applikationens log |

Alle parametre og svar er dokumenteret i Swagger UI på `/swagger`.

## Installation med Docker

### Krav

- En server med [Docker](https://docs.docker.com/engine/install/) og Docker Compose
- Et aktivt betalt medlemskab på [mambeno.dk](https://mambeno.dk)

### 1. Hent koden

```bash
git clone <repo-url> mambeno-api
cd mambeno-api
```

### 2. Opret konfigurationsfilen

```bash
cp .env.example .env
```

Generér en lang tilfældig master-nøgle:

```bash
openssl rand -hex 32
```

Åbn `.env` og indsæt nøglen:

```env
ApiKeys__MasterKey=din-lange-tilfældige-nøgle
```

Master-nøglen giver fuld adgang, også til at oprette og slette andre nøgler. Hold den hemmelig, og brug den ikke i dine klienter.

### 3. Start containeren

```bash
docker compose up -d --build
```

API'et kører nu på port **8080**. Følg med i loggen:

```bash
docker compose logs -f
```

Ved første opstart er databasen tom, og den første synkronisering går i gang med det samme. Det tager et stykke tid, fordi der er bevidst pause mellem forespørgslerne til Mambeno. Indtil den er færdig, svarer `/health` med `503`.

### 4. Opret en API-nøgle til dine klienter

1. Åbn `http://<server>:8080` i en browser. Du sendes videre til Swagger UI.
2. Klik **Authorize**, og indsæt master-nøglen.
3. Kør `POST /Keys/Create` med fx:
   ```json
   { "name": "Min app" }
   ```
4. Kopiér `key` fra svaret. **Nøglen vises kun denne ene gang.**

### 5. Prøv det

```bash
curl -H "x-api-key: <din-nøgle>" "http://<server>:8080/api/v1/recipes?search=lasagne"
```

## Konfiguration

Indstillinger sættes i `.env` og overskriver `appsettings.json`. Navnene bruger `__` i stedet for `:`, så `RecipeSync:DailyRunTime` skrives `RecipeSync__DailyRunTime`. Genstart med `docker compose up -d` efter en ændring.

| Variabel | Standard | Beskrivelse |
|---|---|---|
| `ApiKeys__MasterKey` | *(tom)* | Master-nøglen. **Skal udfyldes.** |
| `RecipeSync__Enabled` | `true` | Slå den natlige synkronisering til/fra |
| `RecipeSync__DailyRunTime` | `03:30:00` | Tidspunkt for den daglige synkronisering |
| `RecipeSync__TimeZone` | `Europe/Copenhagen` | Tidszone for tidspunktet ovenfor |
| `RecipeSync__RetryDelayMinutes` | `60` | Ventetid før nyt forsøg efter en fejl |
| `RecipeSync__MaxDataAgeDays` | `7` | `/health` melder fejl, hvis data er ældre end dette |
| `RecipeSync__RefreshPerRun` | `100` | Antal ældre opskrifter, der genhentes for en sikkerheds skyld pr. kørsel |
| `RecipeSync__MaxDeletionPercent` | `5` | Maks. andel af opskrifterne, der må slettes i én kørsel |
| `RecipeSync__MaxDeletionsAlwaysAllowed` | `10` | Antal sletninger, der altid er tilladt |
| `Mambeno__DelayBetweenRequestsMilliseconds` | `1500` | Pause mellem forespørgsler til Mambeno |
| `Mambeno__RequestTimeoutSeconds` | `60` | Timeout pr. forespørgsel |
| `Mambeno__MaxRetries` | `3` | Antal genforsøg ved midlertidige fejl |

Hele listen står i [.env.example](.env.example). Sæt helst ikke pausen mellem forespørgslerne ned. Den er der af hensyn til Mambenos server.

## Drift

### Data og backup

Databaserne ligger i Docker-volumet `app_dbs` og overlever genstart og genopbygning af containeren. `app.db` indeholder opskrifter og API-nøgler, og `log.db` indeholder loggen.

Lav en backup ved at stoppe containeren kort og kopiere mappen ud:

```bash
docker compose stop
docker compose cp api:/app/app_dbs ./backup-app_dbs
docker compose start
```

### Opdatering

```bash
git pull
docker compose up -d --build
```

Databasen opgraderes automatisk ved opstart.

### HTTPS

Containeren kører kun HTTP. Skal API'et være tilgængeligt udefra, så sæt en reverse proxy foran, fx Caddy, nginx eller Traefik, og lad den håndtere certifikatet.

## Lokal udvikling

Kræver [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project api
```

API'et kører på `http://localhost:5193` med Swagger UI på `/swagger`. Lokale indstillinger, herunder master-nøglen, hører hjemme i `api/appsettings.Development.json` eller `dotnet user-secrets` og må aldrig committes.

## Ansvar og forbehold

Når du installerer eller bruger løsningen, accepterer du følgende:

- **Du skal selv have et betalt medlemskab.** Den, der driver en installation, skal have et aktivt betalt medlemskab på [mambeno.dk](https://mambeno.dk), så længe installationen er i brug. Det er dit eget ansvar at oprette og betale for det.
- **Det er et supplement, ikke en erstatning.** Løsningen giver dig en lokal kopi af opskrifter, du allerede betaler for adgang til. Den må ikke bruges til at få adgang uden medlemskab, til at undgå at betale, eller til at give andre adgang, som ikke selv har et betalt medlemskab.
- **Den er uofficiel.** Projektet er et uafhængigt hobbyprojekt. Det er ikke registreret hos, godkendt, understøttet eller gennemgået af Mambeno, og det har ingen tilknytning til dem. Navnet Mambeno bruges kun til at beskrive, hvilken tjeneste løsningen arbejder sammen med.
- **Du bruger den på eget ansvar.** Du alene er ansvarlig for din installation og for, hvordan du bruger den og de opskrifter, den henter. Det gælder også at overholde Mambenos vilkår, ophavsretten til deres indhold og gældende lov. Udvikleren af projektet har ingen kontrol over andres installationer og påtager sig intet ansvar for dem.
- **Kun til privat brug.** Opskrifterne må kun bruges privat. Du må ikke offentliggøre, sælge eller videregive dem eller tilbyde dem som en tjeneste til andre.
- **Ingen garantier.** Løsningen leveres, som den er. Mambeno kan til enhver tid ændre deres hjemmeside, og så kan løsningen holde op med at virke uden varsel.
- **Krav mod udvikleren.** Hvis nogen, herunder Mambeno, rejser krav mod udvikleren på grund af din brug af løsningen, er det dig, der skal dække det, i det omfang loven tillader det.
- **Projektet kan blive fjernet.** Udvikleren kan til enhver tid stoppe projektet eller fjerne det fra GitHub, fx hvis Mambeno beder om det.

Opskrifterne og andet indhold fra mambeno.dk tilhører Mambeno og er ikke omfattet af licensen.

## Licens

Projektet er udgivet under [Creative Commons Attribution-NonCommercial 4.0](LICENSE.txt) **med tillægsbetingelser**. De gør bl.a. et betalt Mambeno-medlemskab til et krav og placerer ansvaret for brugen hos den, der kører løsningen. Punkterne under [Ansvar og forbehold](#ansvar-og-forbehold) er et dansk resumé. Det er [LICENSE.txt](LICENSE.txt), der gælder.
