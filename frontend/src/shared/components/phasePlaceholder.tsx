import type { Phase } from '../../services/hub/hubContract.ts';
import { useGameStore } from '../../store/gameStore.ts';
import { useCountdown } from '../hooks/useCountdown.ts';
import { Label } from './label.tsx';
import { Marquee } from './marquee.tsx';
import { PausedNotice } from './pausedNotice.tsx';

const TITLES: Record<Phase, string> = {
  lobby: 'Lobbyn',
  submitting: 'Välj en låt',
  playing: 'Lyssna',
  voting: 'Rösta',
  revealing: 'Avslöjande',
  gameOver: 'Spelet är slut',
};

// En tom vy med fasens namn, rundan och nedräkningen, för faser som inte har fått sitt innehåll än.
// #18–#23 tar över en fas i taget, och när den sista är tagen tas platshållaren bort.
export const PhasePlaceholder = ({ screen }: { screen: 'host' | 'player' }) => {
  const game = useGameStore((state) => state.game);
  const seconds = useCountdown();

  if (!game) return null;

  const round = game.phase === 'gameOver' ? 'Slutställning' : `Runda ${game.round} av ${game.settings.rounds}`;

  if (screen === 'host') {
    return (
      <div className='flex min-h-dvh flex-col'>
        <Marquee text={`${TITLES[game.phase]} • ${round}`} />
        <main className='flex flex-1 flex-col justify-center gap-8 px-6 py-12 lg:px-[6vw]'>
          <Label>{round}</Label>
          <h1 className='text-[clamp(3rem,9vw,8rem)] text-display'>{TITLES[game.phase]}</h1>
          {seconds !== null && <p className='text-[clamp(4rem,12vw,12rem)] font-black tabular-nums'>{seconds}</p>}
        </main>
      </div>
    );
  }

  return (
    <main className='flex min-h-dvh flex-col gap-6 px-6 py-12'>
      <Label>{round}</Label>
      <h1 className='text-5xl text-display'>{TITLES[game.phase]}</h1>
      {seconds !== null && <p className='text-6xl font-black tabular-nums'>{seconds}</p>}
      {game.isPaused && <PausedNotice />}
    </main>
  );
};
