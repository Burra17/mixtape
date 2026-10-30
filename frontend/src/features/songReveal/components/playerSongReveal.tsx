import { Countdown } from '../../../shared/components/countdown.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { PausedNotice } from '../../../shared/components/pausedNotice.tsx';
import { placeText } from '../../../shared/components/placeText.ts';
import { TrackRow } from '../../../shared/components/trackRow.tsx';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';
import { resultText } from './resultText.ts';

// Mobilen efter en låt i Drop the Needle: vad du fick och gissade, rätt svar, placeringen och den totala poängen.
export const PlayerSongReveal = () => {
  const session = usePlayerStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();

  const result = game?.answer?.results.find((candidate) => candidate.playerId === session?.playerId);
  const mine = game?.standings?.find((standing) => standing.playerId === session?.playerId);
  if (!game?.answer || !game.standings || !result || !mine) return null;

  const { song } = game.answer;

  return (
    <main className='flex min-h-dvh flex-col gap-6 px-6 py-12'>
      <Label>{round}</Label>
      <h1 className='text-3xl text-display leading-[1.08] [overflow-wrap:anywhere]'>{result.guess ? resultText(result) : 'Du hann inte gissa'}</h1>
      <div className='flex flex-col gap-1'>
        <p className='text-5xl font-black tabular-nums'>+{result.points} poäng</p>
        {result.fastest && <p className='text-lg font-bold'>+1 för snabbast</p>}
      </div>
      {result.guess && (
        <section className='flex flex-col gap-3'>
          <Label>Din gissning</Label>
          <div className='rounded-control border-2 border-ink bg-card p-3 shadow-hard lg:rounded-none'>
            <TrackRow track={result.guess} />
          </div>
        </section>
      )}
      <p className='text-muted'>
        Rätt svar: {song.artist} – {song.title}
      </p>
      <p className='text-2xl font-extrabold'>{placeText(mine, game.standings)}</p>
      <p className='text-lg text-muted'>{mine.score} poäng totalt</p>
      <Countdown screen='player' />
      {game.isPaused && <PausedNotice />}
    </main>
  );
};
