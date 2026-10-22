import { Countdown } from '../../../shared/components/countdown.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { PausedNotice } from '../../../shared/components/pausedNotice.tsx';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// Mobilen under inskicket: prompten och nedräkningen. Sökningen och inskicket kommer i #19.
export const PlayerSubmitting = () => {
  const session = usePlayerStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();

  if (!game?.prompt) return null;

  return (
    <main className='flex min-h-dvh flex-col gap-6 px-6 py-12'>
      <Label>{round}</Label>
      <h1 className='text-3xl text-display [overflow-wrap:anywhere]'>{game.prompt}</h1>
      <Countdown screen='player' />
      {game.isPaused && <PausedNotice />}
    </main>
  );
};
