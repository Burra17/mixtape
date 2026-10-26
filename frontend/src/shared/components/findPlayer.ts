import type { GamePlayer } from '../../services/hub/hubContract.ts';

// Spelaren med id:t i rummets lista, eller undefined. Bilden pekar ut spelare med id, och namn och färg står i players.
export const findPlayer = (players: GamePlayer[], id: string | undefined) => players.find((player) => player.id === id);
