import type { Phase } from '../../services/hub/hubContract.ts';
import { useGameStore } from '../../store/gameStore.ts';
import { useRoundText } from '../hooks/useRoundText.ts';
import { Countdown } from './countdown.tsx';
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

// Tom vy för faser utan innehåll än. #18–#23 tar över en fas i taget, och sedan tas platshållaren bort.
export const PhasePlaceholder = ({ screen }: { screen: 'host' | 'player' }) => {
  const game = useGameStore((state) => state.game);
  const round = useRoundText();

  if (!game) return null;

  if (screen === 'host') {
    return (
      <div className='flex min-h-dvh flex-col'>
        <Marquee text={`${TITLES[game.phase]} • ${round}`} />
        <main className='flex flex-1 flex-col justify-center gap-8 px-6 py-12 lg:px-[6vw]'>
          <Label>{round}</Label>
          <h1 className='text-[clamp(3rem,9vw,8rem)] text-display'>{TITLES[game.phase]}</h1>
          <Countdown screen='host' />
        </main>
      </div>
    );
  }

  return (
    <main className='flex min-h-dvh flex-col gap-6 px-6 py-12'>
      <Label>{round}</Label>
      <h1 className='text-5xl text-display'>{TITLES[game.phase]}</h1>
      <Countdown screen='player' />
      {game.isPaused && <PausedNotice />}
    </main>
  );
};
