import { Countdown } from '../../../shared/components/countdown.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { PlayerChecklist } from '../../../shared/components/playerChecklist.tsx';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';

// Storskärmen under röstningen: nedräkningen och vilka som röstat, men aldrig på vad.
export const HostVoting = () => {
  const session = useHostStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();

  if (!game) return null;

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text={`Rösta • ${round}`} />
      <main className='flex flex-1 flex-col justify-center gap-8 px-6 py-12 lg:px-[6vw]'>
        <Label>{round}</Label>
        <Countdown screen='host' />
        <PlayerChecklist players={game.players} label='Vilka som röstat' isDone={(player) => player.hasVoted} />
      </main>
    </div>
  );
};
