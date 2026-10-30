import { useGameStore } from '../../store/gameStore.ts';

// "Runda 2 av 5" i Aux Cord Battles, "Låt 3 av 10" i Drop the Needle och "Slutställning" efter spelet. Null i lobbyn.
export const useRoundText = () =>
  useGameStore((state) => {
    const game = state.game;
    if (!game || game.phase === 'lobby') return null;
    if (game.phase === 'gameOver') return 'Slutställning';
    if (game.song) return `Låt ${game.song.index} av ${game.song.count}`;
    return game.settings.mode === 'auxCord' ? `Runda ${game.round} av ${game.settings.rounds}` : null;
  });
