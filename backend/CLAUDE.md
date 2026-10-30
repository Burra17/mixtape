# backend/CLAUDE.md

.NET 10, Clean Architecture, Minimal API och SignalR. Reglerna som gäller hela repot står i [../CLAUDE.md](../CLAUDE.md).

## Lager

Beroenden pekar inåt mot Domain, som inte beror på någonting:

- Application → Domain
- Infrastructure → Application
- Api → Application och Infrastructure. Hubben anropar handlers i Application, och Infrastructure refereras bara för att koppla ihop DI.

| Projekt | Innehåll |
|---|---|
| `Mixtape.Domain` | `GameRoom`, `Player`, ett spel per spelläge under `Games/` och spelreglerna. Ingen I/O, inga ramverk. |
| `Mixtape.Application` | En handler per kommando, interfacen som Infrastructure implementerar (`IGameRoomRepository`, `IMusicCatalog`, `IPromptProvider`), och DTO:erna. |
| `Mixtape.Infrastructure` | Rum i minnet, Deezer-klienten, promptbanken och bakgrundstjänster. |
| `Mixtape.Api` | `Program.cs`, `GameHub`, de få HTTP-endpoints som finns, och kopplingen av DI. |
| `Mixtape.Tests` | NUnit. |

Varje lager registrerar sina tjänster i en egen `Add<Lager>()`-metod, och `Program.cs` anropar dem.

## Spelreglerna bor i Domain

`GameRoom` äger det som är gemensamt för alla spellägen: spelarna, värden, lobbyn, klockan, pausen och ställningen. Rummet har tre lägen (`RoomStage`): lobbyn, spelet och slutställningen. Själva spelet är ett `IGame`, till exempel `AuxCordGame`, med egna faser och regler. Rummet har ingen kod per spelläge:

- Varje läge (`GameMode`) har en egen inställningstyp under `Games/`, som `AuxCordSettings`, med sina val och sitt minsta antal spelare (`IGameSettings`). Rummet håller det valda lägets inställningar och minns de andra lägenas under kvällen. Värden byter läge med `room.ChooseMode` och ändrar inställningarna med `room.UpdateSettings<TSettings>(...)`. Standardvalen för ett läge står i `GameModes`.
- `room.StartGame` tar en fabrik som rummet anropar med inställningarna först när värden, lobbyn och antalet spelare är kontrollerade. Fabriken i `StartGameHandler` är det enda stället som kopplar ett läge till sitt spel.
- När en fas är slut svarar spelet med ett `Step`: nästa fas med sin tid, eller att spelet är slut. Rummet startar klockan.
- En spelares kommando går genom `room.Act<TGame>(...)`. Rummet kontrollerar att ett spel av rätt typ pågår och att spelaren finns, räknar upp `Revision` och går vidare direkt när alla är klara.
- Det som inte ändrar bilden, som en fråga om fasen eller en ny klipplänk, går genom `room.WithGame<TGame>(...)`.

Domänobjekten har inga publika setters, och reglerna nås via metoder som `SubmitTrack` och `Vote` på spelet. Varje metod börjar med att kontrollera fasen. Värdens metoder, som `StartGame` och `RemovePlayer`, kontrollerar värd-token före fasen, så att den som inte är värd bara får veta att rummet inte finns.

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
- `GameRoom.Tick(now)` byter fas när deadline passerat, och det är det enda stället där klockan driver spelet framåt. Tick byter också fas när alla anslutna spelare är klara (`IsEveryoneDone`), eftersom den sista som inte var klar kan ha tappat anslutningen.
- En spelares eget anrop, som `SubmitTrack`, byter fas direkt när alla är klara, utan att vänta på nästa tick. Under paus går fasen aldrig vidare i förtid.
- `GameClockService` i Infrastructure anropar `AdvancePhasesHandler` var 250:e ms. Handlern kör `Tick` på varje rum med rummets lås och skickar bilden för de rum som bytt fas via `IGameNotifier`.
- `IGameNotifier` ligger i Application och implementeras i Api med hubbens kontext, eftersom bara Api känner till SignalR.

## Hubben

- `GameHub : Hub<IGameClient>`, så att eventnamn är kompileringsfel och inte stavfel.
- Servern skickar ögonblicksbilder av state, inte små ändrings-events. Den som återansluter behöver då bara den senaste bilden. Hela rummet är en bild, `GameState`, som skickas med `GameStateUpdated` till alla i rummet. Det som bara gäller en spelare går till `player:{id}`.
- `GameState` har tiden som är kvar av fasen (`RemainingMs`), inte en tidpunkt, så att en mobil vars klocka går fel ändå räknar ned rätt.
- Enums skickas som text i camelCase (`"submitting"`), via `JsonStringEnumConverter` i `Program.cs`.
- Det som skiljer sig per spelläge, som `SettingsState` och `ModeOptions`, är en abstrakt record med `[JsonPolymorphic]` och `mode` som diskriminator. I `hubContract.ts` blir den en union som skiljs åt på `mode`.
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
