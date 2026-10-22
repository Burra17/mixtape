import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { useCountdown } from '../../../shared/hooks/useCountdown.ts';
import { useGameStore } from '../../../store/gameStore.ts';

// Storskärmen under inskicket: rundans prompt stort och nedräkningen. Vilka som skickat in kommer i #19.
export const HostSubmitting = () => {
  const game = useGameStore((state) => state.game);
  const seconds = useCountdown();

  if (!game?.prompt) return null;

  const round = `Runda ${game.round} av ${game.settings.rounds}`;

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text={`Välj en låt • ${round}`} />
      <main className='flex flex-1 flex-col justify-center gap-8 px-6 py-12 lg:px-[6vw]'>
        <Label>{round}</Label>
        <h1 className='max-w-[22ch] self-start bg-signal px-4 pt-3 pb-1 text-[clamp(2.5rem,6vw,6.5rem)] text-display'>{game.prompt}</h1>
        {seconds !== null && <p className='text-[clamp(4rem,10vw,10rem)] font-black tabular-nums'>{seconds}</p>}
      </main>
    </div>
  );
};
