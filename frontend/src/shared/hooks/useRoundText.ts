import { useGameStore } from '../../store/gameStore.ts';

// "Runda 2 av 5" under spelet och "Slutställning" efter det. Null i lobbyn.
export const useRoundText = () =>
  useGameStore((state) => {
    const game = state.game;
    if (!game || game.phase === 'lobby') return null;
    return game.phase === 'gameOver' ? 'Slutställning' : `Runda ${game.round} av ${game.settings.rounds}`;
  });
