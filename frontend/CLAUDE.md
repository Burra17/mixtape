# frontend/CLAUDE.md

React, TypeScript, Vite, Tailwind, Zustand och `@microsoft/signalr`. Reglerna som gäller hela repot står i [../CLAUDE.md](../CLAUDE.md).

Routern är `react-router` version 8, och allt importeras från `'react-router'`. `react-router-dom` har ingen version 8, så det paketet används inte.

## Två skärmar

Samma app har två användare med olika förutsättningar:

- **TV:n** (`/host`) körs på laptopen och läses från soffan. Den har stor text, mus och högtalare. Bara TV:n spelar ljud.
- **Mobilen** (`/join`, `/play`) är en handkontroll. Den är stående, har touch och stora knappar, och har inget ljud.

Webbläsaren spelar inte upp ljud förrän någon har klickat på sidan. Klicket på "Starta spel" och "Fortsätt" på TV:n låser upp ljudet.

## Struktur

```text
src/
  features/<fas>/     lobby, submitting, playing, voting, revealing, gameOver
    components/       komponenter för både TV och mobil i den här fasen
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

Riktningen är vald i #31, med We Are Rewind som förebild: varmt off-white, svart text och en gul signalfärg. Stilen är platt, med färgfält, svarta ramar och hårda skuggor. Djup skapas med en förskjuten svart skugga (`shadow-[4px_4px_0_var(--color-ink)]`), inte med glöd, mjuka övertoningar eller oskärpa. Det är det som skiljer stilen från generisk AI-design.

- **Färger:** `paper` (bakgrund), `card` (ytor), `ink` (text och ramar), `muted` (sekundär text) och `signal` (gult, sparsamt: rumskod, prompt, primärknappens text). Spelarna har `player-1` till `player-8`, och namnet står alltid bredvid färgen. Alla färger är tema-variabler i `index.css`, och komponenterna använder dem via klasser som `bg-paper` och `text-signal`.
- **Typografi:** Archivo genomgående. Rubriker, rumskoden och prompten använder `text-display` (full bredd, vikt 900, versaler). Brödtext är vanlig Archivo.
- **Etiketter:** `<Label>` skriver `[RUMSKOD]` i versaler med hakparentes. Använd den för rubriker på paneler och statusrader.
- **Former:** skarpa hörn och 2 px svarta ramar på stor skärm. På mobilen får knappar och fält en liten radie (`rounded-md`), eftersom stora tryckytor ska vara lätta att träffa med tummen.
- **Knappar:** `<Button>` och `<LinkButton>` delar utseende via `buttonStyles.ts`. `primary` är svart med gul text och används en gång per skärm, för det viktigaste steget. Tryckytor är minst 48 px höga.
- **Storlekar på laptopen:** text som ska läsas från soffan skalar med skärmen via `clamp()` och viewport-enheter, så att den fungerar både på laptopen och på en TV via HDMI.
- **Rörelse:** laptopen rör sig hela tiden, med `<Marquee>` och `<Reels>`. Mobilen ligger still. Animationer skrivs som `lg:motion-safe:animate-…`, så att de bara körs på stor skärm och bara när "minska rörelse" är avstängt. `motion-reduce:animate-none` bredvid `lg:animate-…` fungerar inte, eftersom brytpunkten kommer senare i CSS:en och vinner. Stora ögonblick som avslöjandet hör till #24.
- **Layout:** vänsterställt som standard. Centrera bara det som är ensamt i fokus, som rumskoden.

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
