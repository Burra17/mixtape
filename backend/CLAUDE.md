# backend/CLAUDE.md

.NET 10, Clean Architecture, Minimal API och SignalR. Reglerna som gäller hela repot står i [../CLAUDE.md](../CLAUDE.md).

## Lager

Beroenden pekar inåt mot Domain, som inte beror på någonting:

- Application → Domain
- Infrastructure → Application
- Api → Application och Infrastructure. Hubben anropar handlers i Application, och Infrastructure refereras bara för att koppla ihop DI.

| Projekt | Innehåll |
|---|---|
| `Mixtape.Domain` | `GameRoom`, `Player` och spelreglerna. Ingen I/O, inga ramverk. |
| `Mixtape.Application` | En handler per kommando, interfacen som Infrastructure implementerar (`IGameRoomRepository`, `IMusicCatalog`, `IPromptProvider`), och DTO:erna. |
| `Mixtape.Infrastructure` | Rum i minnet, Deezer-klienten, promptbanken och bakgrundstjänster. |
| `Mixtape.Api` | `Program.cs`, `GameHub`, de få HTTP-endpoints som finns, och kopplingen av DI. |
| `Mixtape.Tests` | NUnit. |

Varje lager registrerar sina tjänster i en egen `Add<Lager>()`-metod, och `Program.cs` anropar dem.

## Spelreglerna bor i Domain

`GameRoom` äger sitt state. Domänobjekten har inga publika setters, och reglerna nås via metoder som `SubmitTrack` och `Vote`. Varje metod börjar med att kontrollera fasen.

En regel som säger nej ger ett fel i `Result` (eller `Result<T>` när det lyckade utfallet har ett värde), till exempel "inte i den här fasen", "namnet är upptaget" eller "du kan inte rösta på dig själv". Undantag används för det som inte borde hända, alltså buggar. Ett `Result`-fel blir ett meddelande till spelaren. Ett undantag blir ett fel i loggen.

## Handlers

Varje kommando har en handler: `JoinRoomHandler` med `HandleAsync(...)`. Hubben får handlern injicerad och anropar den direkt. Strukturen är CQRS utan MediatR: samma uppdelning, inget extra lager, och F12 leder rakt in i koden.

Handlern hämtar rummet, anropar domänen och skickar ut resultatet. Den innehåller ingen spelregel.

Hubben är ännu tunnare. Den tar emot anropet, slår upp spelaren via token, anropar handlern och gör om `Result` till `HubResponse<T>`. `Result<T>` kan inte skickas som det är, eftersom `Value` kastar ett undantag när utfallet misslyckats. `HubResponse<T>` har i stället `Value` och `Error` som vanliga fält, och samma form speglas i `hubContract.ts`.

## Samtidighet

`ConcurrentDictionary` skyddar listan med rum, inte själva rummet. Varje ändring av ett `GameRoom` går därför via rummets lås i repositoryt. Utan låset kan två spelare som trycker samtidigt lämna rummet i ett läge som ingen regel tillåter.

## Klockan

Läs tiden från en injicerad `TimeProvider`. Domänen tar emot tiden som argument (`DateTimeOffset now`), och handlers läser den från `TimeProvider`. Testerna har en egen `FakeTimeProvider`. Faserna har deadlines och rum rensas efter inaktivitet, och med `TimeProvider` kan testerna spola fram tiden i stället för att vänta.

Fasernas klocka:

- `PhaseClock` i Domain håller deadline för fasen som pågår. Under paus fryses tiden som är kvar.
- `GameRoom.Tick(now)` byter fas när deadline passerat, och det är det enda stället där klockan driver spelet framåt.
- `GameClockService` i Infrastructure anropar `AdvancePhasesHandler` var 250:e ms. Handlern kör `Tick` på varje rum med rummets lås och skickar bilden för de rum som bytt fas via `IGameNotifier`.
- `IGameNotifier` ligger i Application och implementeras i Api med hubbens kontext, eftersom bara Api känner till SignalR.

## Hubben

- `GameHub : Hub<IGameClient>`, så att eventnamn är kompileringsfel och inte stavfel.
- Servern skickar ögonblicksbilder av state, inte små ändrings-events. Den som återansluter behöver då bara den senaste bilden. Hela rummet är en bild, `GameState`, som skickas med `GameStateUpdated` till alla i rummet. Det som bara gäller en spelare går till `player:{id}`.
- `GameState` har tiden som är kvar av fasen (`RemainingMs`), inte en tidpunkt, så att en mobil vars klocka går fel ändå räknar ned rätt.
- Enums skickas som text i camelCase (`"submitting"`), via `JsonStringEnumConverter` i `Program.cs`.
- Ögonblicksbilden skickas efter att rummets lås släppts, så två bilder kan komma fram i fel ordning. Därför har den rummets `Revision`, och klienten kastar en bild som är äldre än den den har.
- En enskild spelare nås via gruppen `player:{id}`, inte via `ConnectionId`. Anslutningen läggs i gruppen när spelaren går med och när den återansluter.
- Vem en anslutning tillhör står i `IConnectionRegistry`, som bara hubben och handlers använder. Domänen vet bara om en spelare är ansluten (`IsConnected`). Handlers kopplar en ny anslutning med rummets lås, och `DisconnectHandler` kontrollerar andra anslutningar med samma lås, så att en sen disconnect inte markerar en mobil som redan återanslutit.
- Ett anrop som inte ändrar rummet, som `Search`, känner igen spelaren via anslutningen i `IConnectionRegistry` i stället för att klienten skickar sin token. Anslutningen kopplades till spelaren när den gick med eller återanslöt med token, så identiteten bygger fortfarande på token.
- Events och DTO:er är `record`.
- Metoderna i `GameHub` och `IGameClient` har inget `Async`-suffix. SignalR använder metodnamnet som det står som namnet i kontraktet, så `PingAsync` skulle bli det klienten anropar.

## C#-stil

- File-scoped namespaces och en typ per fil
- Klasser är `sealed` om inget annat behövs
- Primary constructors för DI
- `Async`-suffix på asynkrona metoder

`Directory.Build.props` slår på `Nullable` och `TreatWarningsAsErrors`, så en varning stoppar bygget. `.editorconfig` styr formateringen, och `dotnet format` rättar den.

## Tester

- Varje regel i Domain har tester, inklusive kantfallen: fel fas, gränsvärden och dubbletter.
- Handlers testas när de innehåller logik utöver att skicka vidare.
- Ett fåtal integrationstester går mot hubben.
- Testerna går aldrig mot nätet. Ett externt API testas med ett sparat svar i `Fixtures/` och en `FakeHttpHandler`, och hubbtester byter ut tjänsten med `ConfigureTestServices`.
- Testnamnen beskriver beteendet: `Vote_OnOwnEntry_ReturnsError`.
