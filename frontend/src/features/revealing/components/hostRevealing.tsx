import { Countdown } from '../../../shared/components/countdown.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';
import { useShowsStandings } from '../hooks/useShowsStandings.ts';
import { RevealList } from './revealList.tsx';
import { StandingsList } from './standingsList.tsx';

// Storskärmen under avslöjandet: först vem som valde vad, sedan ställningen. Nästa runda startar av sig själv när tiden är ute.
export const HostRevealing = () => {
  const session = useHostStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();
  const showsStandings = useShowsStandings();

  if (!game?.reveal || !game.standings) return null;

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text={`Avslöjande • ${round}`} />
      <main className='flex flex-1 flex-col justify-center gap-8 px-6 py-12 lg:px-[6vw]'>
        <Label>{showsStandings ? 'Ställningen' : round}</Label>
        {showsStandings ? (
          <StandingsList standings={game.standings} players={game.players} />
        ) : (
          <RevealList reveal={game.reveal} players={game.players} />
        )}
        <Countdown screen='host' />
      </main>
    </div>
  );
};
