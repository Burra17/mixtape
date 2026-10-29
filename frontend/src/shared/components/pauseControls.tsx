import { useHostPause } from '../hooks/useHostPause.ts';
import { Button } from './button.tsx';

// Storskärmens paus i faser med nedräkning: en ikonknapp i hörnet, och ett kort över fasen när värden pausat.
// Pausknappen har spelets enda ikon, ritad för hand. När fler ikoner behövs tas ett ikonpaket in.
export const PauseControls = () => {
  const { isPausedByHost, toggle, busy, error } = useHostPause();

  if (!isPausedByHost) {
    return (
      <button
        type='button'
        aria-label='Pausa'
        onClick={() => void toggle()}
        disabled={busy}
        className='fixed right-6 bottom-6 grid size-12 place-items-center border-2 border-ink bg-card shadow-hard transition-transform focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink active:translate-x-0.5 active:translate-y-0.5 active:shadow-hard-pressed'
      >
        <svg viewBox='0 0 20 20' aria-hidden='true' className='size-5 fill-ink'>
          <rect x={3} y={2} width={5} height={16} />
          <rect x={12} y={2} width={5} height={16} />
        </svg>
      </button>
    );
  }

  // Fasen syns kvar bakom kortet, så att soffan ser var spelet stannade. Bakgrunden tonas utan oskärpa, som stilen kräver.
  return (
    <div className='fixed inset-0 grid place-items-center bg-paper/50 px-6'>
      <div role='status' className='flex flex-col items-start gap-8 border-2 border-ink bg-card p-[clamp(2rem,4vw,4rem)] shadow-hard'>
        <h2 className='text-[clamp(4rem,10vw,9rem)] text-display'>Paus</h2>
        <Button variant='primary' onClick={() => void toggle()} disabled={busy}>
          Fortsätt
        </Button>
        {error && <p className='text-lg font-bold'>{error}</p>}
      </div>
    </div>
  );
};
