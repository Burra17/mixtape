# CLAUDE.md

Rewind är ett partyspel för en kväll med kompisar: Aux Cord Battles. TV:n visar en prompt, alla väljer en låt i mobilen, låtarna spelas anonymt och alla röstar. Spelreglerna står i [README.md](README.md).

Allt körs lokalt. Laptopen kör backenden, visar spelet och spelar musiken. Mobilerna ansluter via wifi. Det finns ingen deploy.

Kodreglerna för respektive del står i [backend/CLAUDE.md](backend/CLAUDE.md) och [frontend/CLAUDE.md](frontend/CLAUDE.md). Den här filen har det som gäller hela repot.

## Språk

- Koden skrivs på engelska: klasser, metoder, variabler, filnamn och eventnamn i hubben. Det passar ihop med ramverken.
- Allt som människor läser skrivs på svenska: kommentarer, commits, PR:er, issues, dokumentation och texten i spelet. Texten i spelet skrivs direkt i komponenterna, utan i18n.
- Kommentarer är sparsamma, en rad om avsikten eller skälet. Ett medvetet avsteg från en regel har alltid sitt skäl utskrivet där avsteget sitter. Annars rättas det tillbaka nästa gång någon läser koden.

## Kodprinciper

- KISS och SRP: en klass eller komponent gör en sak, och en kollega förstår den vid första genomläsningen.
- DRY vid tredje upprepningen, inte vid den andra. En abstraktion som byggs för tidigt gissar fel om vad som varierar.
- Bygg det ticketen ber om. Något annat som dyker upp blir en ny ticket.
- Nya paket: fråga först och ge skälet. Paket som står i en ticket är redan godkända.

## Spelets invarianter

Det här gäller både backend och frontend, och en ändring som bryter mot det är fel även om den fungerar:

- **Servern är enda sanningen.** Den äger faserna, klockan, poängen och reglerna. Klienterna visar det servern skickar och skickar tillbaka vad spelaren gjort.
- **Vem som valde vilken låt lämnar aldrig servern** före avslöjandet, inte ens till TV:n. Allt som skickas till en klient går att läsa i nätverksfliken.
- **Spelare och värd identifieras med en hemlig token**, inte med `ConnectionId`. Anslutningen byts varje gång en mobil låser skärmen.
- **Hubkontraktet ändras på båda sidor i samma PR:** `IGameClient` och hubmetoderna i backenden, `hubContract.ts` i frontenden. Kontraktet skrivs för hand, så ingenting annat fångar en sida som glömts.

## Versioner

`global.json` låser .NET SDK, `.nvmrc` låser Node och `yarn.lock` låser paketen. Pakethanteraren är yarn. `npm install` skapar en `package-lock.json` bredvid `yarn.lock`, och då finns det två olika sanningar om vilka versioner som gäller.

## Arbetsflöde

Varje ticket går igenom de här stegen i ordning.

1. **Grilla.** En ny epic grillas på djupet med `/grilling`. En ticket får en snabb genomgång av det som fortfarande är öppet. Klart när ticketen inte har någon öppen fråga kvar.
2. **Brancha** från en uppdaterad `main`: `feature/<issuenummer>`.
3. **Bygg** ticketen och committa löpande. Commits skrivs på svenska i imperativ form.
4. **Claude testar:** testerna är gröna, och appen körs i webbläsarpanelen med en TV-flik och tre spelarflikar när ticketen rör spelet.
5. **André testar** på en riktig mobil.
6. **Granska** med `/code-review` och rätta det som kommer fram.
7. **PR:** titeln är ticketens titel, beskrivningen följer mallen och har `Closes #<nr>`. Ett val som inte är självklart får sitt skäl i beskrivningen.
8. **Merga** med squash när André säger till, aldrig innan. Flytta ticketen till Done.

Förklara kort medan du bygger, och bara när ett val inte är självklart.

### Klar-kriterier

En ticket är klar när allt det här stämmer:

- CI är grön. Kör samma kontroller lokalt före PR, de står i `.github/workflows/ci.yml`.
- Domänreglerna som ticketen rör har tester.
- Claude och André har testat enligt steg 4 och 5.
- `/code-review` är körd.
- Dokumentationen är uppdaterad i samma PR om ett mönster eller en regel ändrats. Ett dokument som beskriver en struktur projektet vuxit ifrån läses som sanning.

### Git

`main` är skyddad. Allt går via PR, och CI måste vara grön. Det gäller även repoägaren, för en grind man kan kliva över är ingen grind. En PR håller sig till en ticket.
