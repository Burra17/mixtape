import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { useCountdown } from '../../../shared/hooks/useCountdown.ts';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';
import { useClipPlayback } from '../hooks/useClipPlayback.ts';
import { Vinyl } from './vinyl.tsx';

// Storskärmen under uppspelningen: omslaget, låten och sekunderna kvar av klippet. Aldrig vem som valde låten.
// Från lg sticker en vinylskiva ut en tredjedel ur omslaget, som ur ett fodral, och snurrar medan klippet spelas.
export const HostPlaying = () => {
  const session = useHostStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();
  const seconds = useCountdown();
  const { failed, playing } = useClipPlayback();

  const clip = game?.nowPlaying;
  if (!clip) return null;

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text={`Lyssna • ${round}`} />
      <main className='flex flex-1 flex-col justify-center gap-12 px-6 py-12 lg:px-[6vw]'>
        <div className='flex flex-col gap-10 lg:flex-row lg:items-center'>
          <div className='relative shrink-0 lg:pr-[calc(min(38vw,60vh)/3)]'>
            {/* key startar om skivan från början när ett nytt klipp börjar. */}
            <Vinyl key={clip.index} spinning={playing} className='absolute top-0 right-0 hidden w-[min(38vw,60vh)] lg:block' />
            <img src={clip.coverUrl} alt='' className='relative aspect-square w-[min(38vw,60vh)] border-2 border-ink bg-card shadow-hard' />
          </div>
          <div className='flex min-w-0 flex-col gap-4'>
            <Label>
              Låt {clip.index} av {clip.count}
            </Label>
            <h1 className='text-[clamp(2.5rem,5vw,5.5rem)] text-display leading-[1.08] [overflow-wrap:anywhere]'>{clip.title}</h1>
            <p className='text-[clamp(1.25rem,2vw,2rem)] font-bold'>{clip.artist}</p>
            <p className='text-[clamp(1rem,1.5vw,1.5rem)] text-muted'>{clip.album}</p>
            {failed && (
              <p role='status' className='text-[clamp(1rem,1.5vw,1.5rem)] font-bold'>
                Klippet gick inte att spela.
              </p>
            )}
            {seconds !== null && <p className='text-[clamp(2rem,4vw,4rem)] font-black tabular-nums'>{seconds}</p>}
          </div>
        </div>
        <footer className='flex flex-wrap items-end justify-between gap-4 text-[clamp(1rem,1.4vw,1.4rem)]'>
          <p className='font-bold'>{game.prompt}</p>
          <p className='text-muted'>Klipp från Deezer</p>
        </footer>
      </main>
    </div>
  );
};
