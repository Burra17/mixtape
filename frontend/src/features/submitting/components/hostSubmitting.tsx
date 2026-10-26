import { Countdown } from '../../../shared/components/countdown.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { PlayerChecklist } from '../../../shared/components/playerChecklist.tsx';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';

// Storskärmen under inskicket: rundans prompt stort, nedräkningen och vilka som skickat in, men aldrig vilken låt.
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
        <h1 className='max-w-[22ch] self-start bg-signal px-4 pt-4 pb-2 text-[clamp(2.5rem,6vw,6.5rem)] text-display leading-[1.08]'>
          {game.prompt}
        </h1>
        {game.skippedRound && (
          <p role='status' className='text-[clamp(1.25rem,1.8vw,1.75rem)] font-bold'>
            Förra rundan hoppades över – för få bidrag.
          </p>
        )}
        <Countdown screen='host' />
        <PlayerChecklist players={game.players} label='Vilka som skickat in' isDone={(player) => player.hasSubmitted} />
      </main>
    </div>
  );
};
