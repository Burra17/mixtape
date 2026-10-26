import { Countdown } from '../../../shared/components/countdown.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { PlayerChecklist } from '../../../shared/components/playerChecklist.tsx';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';
import { HostBallot } from './hostBallot.tsx';

// Storskärmen under röstningen: prompten, bidragen och vilka som röstat, men aldrig på vad eller hur många röster ett bidrag fått.
export const HostVoting = () => {
  const session = useHostStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();

  if (!game) return null;

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text={`Rösta • ${round}`} />
      {/* Tätare än de andra faserna, så att 8 bidrag ryms utan scroll även på en 768 px hög laptopskärm. */}
      <main className='flex flex-1 flex-col justify-center gap-6 px-6 py-8 lg:px-[6vw]'>
        <div className='flex items-start justify-between gap-8'>
          <div className='flex flex-col gap-4'>
            <Label>{round}</Label>
            <h1 className='max-w-[26ch] self-start bg-signal px-3 pt-3 pb-1 text-[clamp(1.5rem,3vw,3rem)] text-display leading-[1.08]'>
              {game.prompt}
            </h1>
          </div>
          <Countdown screen='host' />
        </div>
        {game.ballot && <HostBallot entries={game.ballot} />}
        <PlayerChecklist players={game.players} label='Vilka som röstat' isDone={(player) => player.hasVoted} />
      </main>
    </div>
  );
};
