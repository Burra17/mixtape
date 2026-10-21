import { create } from 'zustand';
import type { GameState } from '../services/hub/hubContract.ts';

type GameStoreState = {
  game: GameState | null;
  // När bilden kom fram enligt den här enhetens klocka. Tillsammans med remainingMs blir det fasens deadline.
  receivedAt: number;
  receiveGame: (game: GameState) => void;
  reset: () => void;
};

// Serverns senaste bild av rummet, samma för storskärmen och mobilen.
export const useGameStore = create<GameStoreState>()((set) => ({
  game: null,
  receivedAt: 0,
  // Bilderna kan komma i fel ordning, så en äldre bild av samma rum kastas.
  receiveGame: (game) =>
    set((state) => (state.game?.roomCode === game.roomCode && state.game.revision > game.revision ? state : { game, receivedAt: Date.now() })),
  // Före ett nytt rum, så att ett rum som råkar få samma kod som det förra inte får sina första bilder kastade.
  reset: () => set({ game: null }),
}));
