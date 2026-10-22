import { Label } from '../../../shared/components/label.tsx';
import { PausedNotice } from '../../../shared/components/pausedNotice.tsx';
import { useRoomGame } from '../../../store/gameStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// Mobilen under uppspelningen: vilken låt TV:n spelar. Ingen nedräkning, eftersom den skulle börja om för varje klipp.
export const PlayerPlaying = () => {
  const session = usePlayerStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);

  const clip = game?.nowPlaying;
  if (!clip) return null;

  return (
    <main className='flex min-h-dvh flex-col gap-6 px-6 py-12'>
      <p className='font-bold [overflow-wrap:anywhere]'>{game.prompt}</p>
      <Label>
        Låt {clip.index} av {clip.count}
      </Label>
      <img src={clip.coverUrl} alt='' className='aspect-square w-1/2 rounded-control border-2 border-ink bg-card shadow-hard lg:rounded-none' />
      <div className='flex flex-col gap-1'>
        <p className='text-2xl font-black [overflow-wrap:anywhere]'>{clip.title}</p>
        <p className='text-lg text-muted'>{clip.artist}</p>
      </div>
      {game.isPaused && <PausedNotice />}
    </main>
  );
};
