import { Countdown } from '../../../shared/components/countdown.tsx';
import { findPlayer } from '../../../shared/components/findPlayer.ts';
import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { PlayerChip } from '../../../shared/components/playerChip.tsx';
import { Sleeve } from '../../../shared/components/sleeve.tsx';
import { useClipPlayback } from '../../../shared/hooks/useClipPlayback.ts';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';
import { resultText } from './resultText.ts';

// Storskärmen efter en låt i Drop the Needle: rätt svar med omslaget och skivan, och vad varje spelare fick, sorterat efter ställningen.
// Allt syns i en vy, eftersom avslöjandet bara är 10 s. Klippet spelar vidare där det var, och nästa låt startar av sig själv.
export const HostSongReveal = () => {
  const session = useHostStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();
  const { playing } = useClipPlayback();

  const answer = game?.answer;
  if (!answer || !game.standings || !game.song) return null;

  const nobody = answer.results.every((result) => result.score === 'none');

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text={`Avslöjande • ${round}`} />
      <main className='flex flex-1 flex-col justify-center gap-10 px-6 py-12 lg:flex-row lg:items-center lg:gap-[4vw] lg:px-[6vw]'>
        <section className='flex min-w-0 flex-col gap-6 lg:w-[40%] lg:shrink-0'>
          {/* key startar om skivan när nästa låt avslöjas. */}
          <Sleeve key={game.song.clip} coverUrl={answer.song.coverUrl} spinning={playing} className='[--cover:min(24vw,40vh)]' />
          <div className='flex min-w-0 flex-col gap-2'>
            <Label>{round}</Label>
            <h1 className='text-[clamp(2rem,3.6vw,4rem)] text-display leading-[1.08] [overflow-wrap:anywhere]'>{answer.song.title}</h1>
            <p className='text-[clamp(1.25rem,2vw,2rem)] font-bold'>{answer.song.artist}</p>
          </div>
        </section>
        <section className='flex min-w-0 flex-1 flex-col gap-6'>
          {nobody && <p className='text-[clamp(2rem,4vw,4.5rem)] text-display leading-[1.1]'>Ingen fick den!</p>}
          <ol aria-label='Vad alla fick' className='flex flex-col gap-4 text-[clamp(1.1rem,1.7vw,1.7rem)]'>
            {game.standings.map((standing) => {
              const player = findPlayer(game.players, standing.playerId);
              const result = answer.results.find((candidate) => candidate.playerId === standing.playerId);
              return (
                <li key={standing.playerId} className='flex items-center gap-5'>
                  <span className='w-[1.6em] shrink-0 font-black tabular-nums'>{standing.place}.</span>
                  <span className='min-w-0 flex-1'>{player && <PlayerChip player={player} />}</span>
                  {result && (
                    <span className='flex shrink-0 items-center gap-3'>
                      <span className={result.score === 'none' ? 'text-muted' : 'font-extrabold'}>{resultText(result)}</span>
                      {result.fastest && <span className='border-2 border-ink px-2 text-[0.7em] font-black uppercase'>Snabbast</span>}
                    </span>
                  )}
                  <span className='w-[2.5em] shrink-0 text-right font-bold text-muted tabular-nums'>+{result?.points ?? 0}</span>
                  <span className='w-[3.5em] shrink-0 text-right font-black tabular-nums'>{standing.score} p</span>
                </li>
              );
            })}
          </ol>
          <Countdown screen='host' />
        </section>
      </main>
    </div>
  );
};
