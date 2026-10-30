import { Countdown } from '../../../shared/components/countdown.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { PlayerChecklist } from '../../../shared/components/playerChecklist.tsx';
import { Vinyl } from '../../../shared/components/vinyl.tsx';
import { categoryName, useCategories } from '../../../shared/hooks/useCategories.ts';
import { useClipPlayback } from '../../../shared/hooks/useClipPlayback.ts';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';

// Storskärmen medan alla gissar: skivan med ett frågetecken, låtens nummer, kategorin, nedräkningen och vilka som gissat.
// Aldrig titel, artist eller omslag, eftersom det är det som ska gissas.
export const HostGuessing = () => {
  const session = useHostStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();
  const categories = useCategories();
  const { failed, playing } = useClipPlayback();

  const song = game?.song;
  if (!song) return null;

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text={`Gissa låten • ${round}`} />
      <main className='flex flex-1 flex-col justify-center gap-12 px-6 py-12 lg:px-[6vw]'>
        <div className='flex flex-col gap-10 lg:flex-row lg:items-center'>
          {/* key startar om skivan från början när en ny låt börjar. */}
          <Vinyl key={song.clip} spinning={playing} className='w-[min(38vw,60vh)] shrink-0' />
          <div className='flex min-w-0 flex-col gap-4'>
            <Label>{round}</Label>
            <h1 className='text-[clamp(2.5rem,5vw,5.5rem)] text-display leading-[1.08]'>{categoryName(categories, song.categoryId)}</h1>
            {failed && (
              <p role='status' className='text-[clamp(1rem,1.5vw,1.5rem)] font-bold'>
                Klippet gick inte att spela.
              </p>
            )}
            <Countdown screen='host' />
          </div>
        </div>
        <footer className='flex flex-wrap items-end justify-between gap-6'>
          <PlayerChecklist players={game.players} label='Vilka som gissat' isDone={(player) => player.hasGuessed} />
          <p className='text-[clamp(1rem,1.4vw,1.4rem)] text-muted'>Klipp från Deezer</p>
        </footer>
      </main>
    </div>
  );
};
