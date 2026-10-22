import { Label } from '../../../shared/components/label.tsx';
import { PausedNotice } from '../../../shared/components/pausedNotice.tsx';
import { useCountdown } from '../../../shared/hooks/useCountdown.ts';
import { useGameStore } from '../../../store/gameStore.ts';

// Mobilen under inskicket: prompten och nedräkningen. Sökningen och inskicket kommer i #19.
export const PlayerSubmitting = () => {
  const game = useGameStore((state) => state.game);
  const seconds = useCountdown();

  if (!game?.prompt) return null;

  return (
    <main className='flex min-h-dvh flex-col gap-6 px-6 py-12'>
      <Label>
        Runda {game.round} av {game.settings.rounds}
      </Label>
      <h1 className='text-3xl text-display [overflow-wrap:anywhere]'>{game.prompt}</h1>
      {seconds !== null && <p className='text-6xl font-black tabular-nums'>{seconds}</p>}
      {game.isPaused && <PausedNotice />}
    </main>
  );
};
