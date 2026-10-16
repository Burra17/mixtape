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

## Styling

Tailwind-klasser direkt i JSX. Färger och typsnitt definieras som tema-variabler en gång i `index.css`, och komponenterna använder dem via klassnamn.

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
