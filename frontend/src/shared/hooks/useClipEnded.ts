import { useGameStore } from '../../store/gameStore.ts';
import { useCountdown } from './useCountdown.ts';

// Sant under Drop the Needles betänketid: klippet har spelat klart, men det går fortfarande att gissa.
export const useClipEnded = () => {
  const seconds = useCountdown();
  const thinkingSeconds = useGameStore((state) =>
    state.game?.phase === 'guessing' && state.game.settings.mode === 'dropTheNeedle' ? state.game.settings.thinkingSeconds : 0,
  );
  return thinkingSeconds > 0 && seconds !== null && seconds <= thinkingSeconds;
};
