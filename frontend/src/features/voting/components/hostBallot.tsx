import type { BallotEntry } from '../../../services/hub/hubContract.ts';

// Storskärmens röstsedel: bidragen som kort, tre per rad, så att 8 bidrag ryms utan att sidan scrollar. Numret är stort, eftersom det är det folk säger högt och väljer i mobilen.
export const HostBallot = ({ entries }: { entries: BallotEntry[] }) => (
  <ol aria-label='Bidragen' className='grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3'>
    {entries.map((entry) => (
      <li key={entry.number} className='flex min-w-0 items-center gap-4 border-2 border-ink bg-card p-3 shadow-hard'>
        <span className='w-[1.2em] shrink-0 text-[clamp(2rem,3.5vw,3.5rem)] font-black tabular-nums'>{entry.number}</span>
        <img src={entry.coverUrl} alt='' className='aspect-square w-[clamp(3.5rem,5.5vw,6rem)] shrink-0 border-2 border-ink bg-paper' />
        <span className='flex min-w-0 flex-col text-[clamp(1rem,1.4vw,1.5rem)]'>
          <span className='line-clamp-2 font-extrabold [overflow-wrap:anywhere]'>{entry.title}</span>
          <span className='truncate text-muted'>{entry.artist}</span>
        </span>
      </li>
    ))}
  </ol>
);
