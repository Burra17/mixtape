import { useGameStore } from '../../store/gameStore.ts';

// "Runda 2 av 5" under spelet och "Slutställning" efter det. Null i lobbyn.
export const useRoundText = () =>
  useGameStore((state) => {
    const game = state.game;
    if (!game || game.phase === 'lobby') return null;
    if (game.phase === 'gameOver') return 'Slutställning';
    // Bara Aux Cord Battles har rundor. Drop the Needle får sin egen rad i #70.
    return game.settings.mode === 'auxCord' ? `Runda ${game.round} av ${game.settings.rounds}` : null;
  });
