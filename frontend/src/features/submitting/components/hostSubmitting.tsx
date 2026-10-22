import { Countdown } from '../../../shared/components/countdown.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { PlayerChip } from '../../../shared/components/playerChip.tsx';
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
        <ul aria-label='Vilka som skickat in' className='flex flex-wrap gap-x-10 gap-y-4 text-[clamp(1.25rem,1.8vw,1.75rem)]'>
          {game.players.map((player) => (
            <li key={player.id} className='flex items-center gap-3'>
              {/* Nedtoningen syns dåligt på avstånd, så bocken och ordet "borta" står bredvid. */}
              <span className={player.hasSubmitted ? '' : 'opacity-40'}>
                <PlayerChip player={player} />
              </span>
              {player.hasSubmitted && (
                <span aria-label='klar' className='font-black'>
                  ✓
                </span>
              )}
              {!player.isConnected && <span className='text-[0.7em] font-bold tracking-label text-muted uppercase'>borta</span>}
            </li>
          ))}
        </ul>
      </main>
    </div>
  );
};
