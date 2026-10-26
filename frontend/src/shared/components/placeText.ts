import type { GameStanding } from '../../services/hub/hubContract.ts';

// "1:a" och "2:a", annars ":e", som "3:e" och "8:e".
const ordinal = (place: number) => `${place}:${place === 1 || place === 2 ? 'a' : 'e'}`;

// Någon annan har samma placering.
export const isSharedPlace = (mine: GameStanding, standings: GameStanding[]) =>
  standings.filter((standing) => standing.place === mine.place).length > 1;

// "2:a av 4", eller "Delad 1:a av 4" när någon annan har samma placering.
export const placeText = (mine: GameStanding, standings: GameStanding[]) =>
  `${isSharedPlace(mine, standings) ? 'Delad ' : ''}${ordinal(mine.place)} av ${standings.length}`;
