import { useState } from 'react';
import { useEndGame } from '../hooks/useEndGame.ts';
import { useHostPause } from '../hooks/useHostPause.ts';
import { Button } from './button.tsx';
import { iconButtonClasses } from './buttonStyles.ts';

// Storskärmens paus i faser med nedräkning: en ikonknapp i hörnet, och ett kort över fasen när värden pausat.
export const PauseControls = () => {
  const { isPausedByHost, toggle, busy, error } = useHostPause();

  if (!isPausedByHost) {
    return (
      <div className='fixed right-6 bottom-6 flex items-center gap-4'>
        {error && (
          <p role='alert' className='border-2 border-ink bg-card px-3 py-2 font-bold'>
            {error}
          </p>
        )}
        <button type='button' aria-label='Pausa' onClick={() => void toggle()} disabled={busy} className={iconButtonClasses}>
          <svg viewBox='0 0 20 20' aria-hidden='true' className='size-5 fill-ink'>
            <rect x={3} y={2} width={5} height={16} />
            <rect x={12} y={2} width={5} height={16} />
          </svg>
        </button>
      </div>
    );
  }

  return <PauseCard onContinue={() => void toggle()} busy={busy} error={error} />;
};

type PauseCardProps = {
  onContinue: () => void;
  busy: boolean;
  error: string | null;
};

// Fasen syns kvar bakom kortet, så att soffan ser var spelet stannade. Bakgrunden tonas utan oskärpa, som stilen kräver.
// Att avsluta går inte att ångra, så "Avsluta spelet" byts mot en bekräftelse i samma kort.
const PauseCard = ({ onContinue, busy, error }: PauseCardProps) => {
  const [confirming, setConfirming] = useState(false);
  const { end, busy: ending, error: endError } = useEndGame();

  return (
    <div className='fixed inset-0 grid place-items-center bg-paper/50 px-6'>
      <div role='status' className='flex flex-col items-start gap-8 border-2 border-ink bg-card p-[clamp(2rem,4vw,4rem)] shadow-hard'>
        <h2 className='text-[clamp(4rem,10vw,9rem)] text-display'>Paus</h2>
        <Button variant='primary' onClick={onContinue} disabled={busy || ending}>
          Fortsätt
        </Button>
        {confirming ? (
          <div className='flex flex-col items-start gap-4'>
            <p className='text-xl font-bold'>Säker? Poängen hittills blir slutställning.</p>
            <div className='flex gap-4'>
              <Button onClick={() => void end()} disabled={ending}>
                Ja, avsluta
              </Button>
              <Button onClick={() => setConfirming(false)} disabled={ending}>
                Avbryt
              </Button>
            </div>
          </div>
        ) : (
          <Button onClick={() => setConfirming(true)} disabled={busy}>
            Avsluta spelet
          </Button>
        )}
        {(error ?? endError) && <p className='text-lg font-bold'>{error ?? endError}</p>}
      </div>
    </div>
  );
};
