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

Hubben är ännu tunnare. Den tar emot anropet, slår upp spelaren via token, anropar handlern och returnerar `Result`.

## Samtidighet

`ConcurrentDictionary` skyddar listan med rum, inte själva rummet. Varje ändring av ett `GameRoom` går därför via rummets lås i repositoryt. Utan låset kan två spelare som trycker samtidigt lämna rummet i ett läge som ingen regel tillåter.

## Klockan

Läs tiden från en injicerad `TimeProvider`. Faserna har deadlines och rum rensas efter inaktivitet, och med `TimeProvider` kan testerna spola fram tiden i stället för att vänta.

## Hubben

- `GameHub : Hub<IGameClient>`, så att eventnamn är kompileringsfel och inte stavfel.
- Servern skickar ögonblicksbilder av state (`LobbyUpdated`, `GameStateUpdated`), inte små ändrings-events. Den som återansluter behöver då bara den senaste bilden.
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
- Testnamnen beskriver beteendet: `Vote_OnOwnEntry_ReturnsError`.
