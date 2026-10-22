import { Countdown } from '../../../shared/components/countdown.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';

// Storskärmen under inskicket: rundans prompt stort och nedräkningen. Vilka som skickat in kommer i #19.
export const HostSubmitting = () => {
  const session = useHostStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();

  if (!game?.prompt) return null;

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text={`Välj en låt • ${round}`} />
      <main className='flex flex-1 flex-col justify-center gap-8 px-6 py-12 lg:px-[6vw]'>
        <Label>{round}</Label>
        <h1 className='max-w-[22ch] self-start bg-signal px-4 pt-3 pb-1 text-[clamp(2.5rem,6vw,6.5rem)] text-display'>{game.prompt}</h1>
        <Countdown screen='host' />
      </main>
    </div>
  );
};
