import { Button } from '../../../shared/components/button.tsx';
import { Countdown } from '../../../shared/components/countdown.tsx';
import { ErrorLine } from '../../../shared/components/errorLine.tsx';
import { findPlayer } from '../../../shared/components/findPlayer.ts';
import { PausedNotice } from '../../../shared/components/pausedNotice.tsx';
import { useIsConnected } from '../../../store/connectionStore.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';
import { useBallot } from '../hooks/useBallot.ts';
import { BallotRow } from './ballotRow.tsx';

// Mobilen under röstningen: bidragen i uppspelningens ordning. Ett tryck markerar och "Rösta" bekräftar, eftersom rösten är låst efteråt.
export const PlayerVoting = () => {
  const session = usePlayerStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const isConnected = useIsConnected();
  const { ownEntry, votedEntry, ballotError, selected, select, error, busy, castVote } = useBallot();

  if (!game?.ballot) return null;

  // Servern säger om spelaren har röstat, så att "Röst lagd" står kvar efter en omladdning.
  const hasVoted = findPlayer(game.players, session?.playerId)?.hasVoted ?? false;
  const voted = game.ballot.find((entry) => entry.number === votedEntry);

  return (
    <main className='flex min-h-dvh flex-col gap-6 px-6 pt-12'>
      <p className='font-bold [overflow-wrap:anywhere]'>{game.prompt}</p>
      <Countdown screen='player' />
      {game.isPaused && <PausedNotice />}
      {ballotError && <ErrorLine error={ballotError} />}

      {hasVoted ? (
        <section className='flex flex-col gap-4 pb-12'>
          <p role='status' className='text-lg font-bold'>
            Röst lagd
          </p>
          {voted && (
            <div className='rounded-control border-2 border-ink bg-card p-3 shadow-hard lg:rounded-none'>
              <BallotRow entry={voted} />
            </div>
          )}
          <p className='font-medium text-muted'>Väntar på de andra.</p>
        </section>
      ) : (
        <>
          <ul aria-label='Bidragen' className='flex flex-col gap-2'>
            {game.ballot.map((entry) => {
              const isOwn = entry.number === ownEntry;
              const isSelected = entry.number === selected;
              return (
                <li key={entry.number}>
                  <button
                    type='button'
                    aria-pressed={isSelected}
                    disabled={isOwn}
                    onClick={() => select(isSelected ? null : entry.number)}
                    className={`flex min-h-12 w-full rounded-control border-2 p-2 focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink disabled:opacity-40 lg:rounded-none ${isSelected ? 'border-ink bg-card shadow-hard' : 'border-transparent'}`}
                  >
                    <BallotRow entry={entry} isOwn={isOwn} />
                  </button>
                </li>
              );
            })}
          </ul>

          {/* Knappen ligger kvar längst ned, som "Skicka in" i inskicket. */}
          <div className='sticky bottom-0 -mx-6 flex flex-col gap-3 border-t-2 border-ink bg-paper px-6 py-4'>
            {error && <ErrorLine error={error} />}
            <Button variant='primary' onClick={() => void castVote()} disabled={busy || !isConnected || selected === null}>
              Rösta
            </Button>
          </div>
        </>
      )}
    </main>
  );
};
