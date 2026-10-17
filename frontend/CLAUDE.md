# frontend/CLAUDE.md

React, TypeScript, Vite, Tailwind, Zustand och `@microsoft/signalr`. Reglerna som gäller hela repot står i [../CLAUDE.md](../CLAUDE.md).

Routern är `react-router` version 8, och allt importeras från `'react-router'`. `react-router-dom` har ingen version 8, så det paketet används inte.

## Två skärmar

Samma app har två användare med olika förutsättningar:

- **Storskärmen** (`/host`) är laptopen, eller en TV som är kopplad till den via HDMI. Den läses på avstånd och har stor text, mus och högtalare. Bara storskärmen spelar ljud.
- **Mobilen** (`/join`, `/play`) är en handkontroll. Den är stående, har touch och stora knappar, och har inget ljud.

Brytpunkten `lg` (1024 px) är gränsen mellan dem i stilen: från `lg` och uppåt gäller storskärmens former och rörelse.

Webbläsaren spelar inte upp ljud förrän någon har klickat på sidan. Klicket på "Starta spel" och "Fortsätt" på storskärmen låser upp ljudet.

## Struktur

```text
src/
  features/<fas>/     lobby, submitting, playing, voting, revealing, gameOver
    components/       komponenter för både storskärm och mobil i den här fasen
  router.tsx          en route per vy
  views/              en vy per route. hostView och playerView är tunna skal som väljer fas utifrån serverns state
  services/hub/       anslutningen, hubContract.ts och alla anrop till hubben
  store/              Zustand-storen
  shared/components/  komponenter som används av flera faser
```

Mapparna heter som faserna i `Phase` nedan, så att fasen i serverns state och mappen med dess komponenter har samma namn. En ticket om en fas berör bara den fasens mapp. Behöver en andra fas en komponent flyttas den till `shared/components/`. Flytta, kopiera inte.

Skapa mappar när de fylls, inte i förväg.

## Dataflöde

```text
hubb-event → store → komponent → hubbanrop via services/hub → server
```

- All kommunikation med servern går genom `services/hub/`. Komponenterna anropar funktioner därifrån.
- Hubbens events skriver serverns ögonblicksbild till storen. Komponenterna läser ur storen med selectors.
- Klienten håller bara UI-state, som texten i ett sökfält. Faser, poäng och regler kommer från servern.

## Stil

Riktningen är vald i #31, med We Are Rewind som förebild: varmt off-white, svart text och en gul signalfärg. Stilen är platt, med färgfält, svarta ramar och hårda skuggor (`shadow-hard`). Djup skapas aldrig med glöd, mjuka övertoningar eller oskärpa, och det är det som skiljer stilen från generisk AI-design. Allt som stilen bygger på är tema-variabler i `index.css`, och komponenterna använder dem via klasser.

- **Färger:** `paper` (bakgrund), `card` (ytor), `ink` (text, ramar och skuggor), `muted` (sekundär text) och `signal` (gult, sparsamt: textbandet, rumskoden, prompten och primärknappens text).
- **Spelare:** varje spelare har en av `player-1` till `player-8`. En spelare visas som en färgruta med initialen i vit text, och namnet i `ink` bredvid. Namnet står alltid med, så att ingen behöver skilja spelarna åt bara på färgen.
- **Typografi:** Archivo genomgående. Rubriker, rumskoden och prompten använder `text-display` (full bredd, vikt 900, versaler). Brödtext är vanlig Archivo.
- **Etiketter:** `<Label>` skriver `[RUMSKOD]` i versaler med hakparentes. Använd den för rubriker på paneler och statusrader.
- **Paneler:** `<Card>` för det som hör ihop, som rumskoden eller spelarlistan.
- **Former:** ramar är `border-2 border-ink`. På storskärmen är hörnen skarpa. På mobilen får knappar, fält och paneler `rounded-control`, eftersom stora tryckytor ska vara lätta att träffa med tummen. Mönstret är `rounded-control lg:rounded-none`.
- **Knappar:** `<Button>` och `<LinkButton>` delar utseende via `buttonStyles.ts`. `primary` är svart med gul text och används en gång per skärm, för det viktigaste steget. Tryckytor är minst 48 px höga (`min-h-12`).
- **Fält:** `bg-card`, `border-2 border-ink`, `min-h-12` och samma radie som knapparna. Fokus visas med samma ram som knapparnas (`outline-ink`).
- **Storlekar på storskärmen:** text i `/host` som ska läsas på avstånd skalar med skärmen via `clamp()` och viewport-enheter. Startsidan och mobilens vyer använder fasta storlekar.
- **Rörelse:** storskärmen rör sig hela tiden, med `<Marquee>` och `<Reels>`. Mobilen ligger still. Animationer skrivs som `lg:motion-safe:animate-…`, så att de bara körs från `lg` och bara när "minska rörelse" är avstängt. `motion-reduce:animate-none` bredvid `lg:animate-…` fungerar inte, eftersom brytpunkten kommer senare i CSS:en och vinner. Stora ögonblick som avslöjandet hör till #24.
- **Layout:** vänsterställt som standard, som hos förebilden. Centrera bara det som är ensamt i fokus, som rumskoden.

## Namngivning

- Filer: camelCase, som `voteList.tsx` och `useCountdown.ts`
- Komponenter, typer och interface: PascalCase
- Komponenter är arrow functions med namngiven export: `export const VoteList = () => {}`
- `App.tsx` och `main.tsx` är Vites egna filer och behåller sina namn

## TypeScript-regler som biter

Vites mall slår på inställningar som gör vanliga mönster till byggfel:

- `verbatimModuleSyntax`: typer importeras med `import type { GameState } from '...'`.
- `erasableSyntaxOnly`: `enum` och `namespace` går inte. Använd en union eller `as const`:

  ```ts
  export const PHASES = ['lobby', 'submitting', 'playing', 'voting', 'revealing', 'gameOver'] as const;
  export type Phase = (typeof PHASES)[number];
  ```

- `noUnusedLocals` och `noUnusedParameters`: en oanvänd variabel stoppar bygget.

`yarn build` kör `tsc -b` före Vite-bygget och är den riktiga typkontrollen. Att `yarn dev` startar betyder inte att koden kompilerar.

Prettier sköter formateringen.
