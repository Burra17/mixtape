import type { BallotEntry } from '../../../services/hub/hubContract.ts';

type BallotRowProps = {
  entry: BallotEntry;
  isOwn?: boolean;
};

// Ett bidrag i röstningen: numret från uppspelningen, omslaget, titeln och artisten. Det egna bidraget märks med "Din låt".
export const BallotRow = ({ entry, isOwn = false }: BallotRowProps) => (
  <span className='flex min-w-0 items-center gap-3 text-left'>
    <span className='w-6 shrink-0 text-xl font-black tabular-nums'>{entry.number}</span>
    <img src={entry.coverUrl} alt='' width={56} height={56} className='size-14 shrink-0 border-2 border-ink bg-paper' />
    <span className='flex min-w-0 flex-col'>
      <span className='truncate font-extrabold'>{entry.title}</span>
      <span className='truncate text-muted'>{entry.artist}</span>
    </span>
    {isOwn && <span className='ml-auto shrink-0 text-sm font-bold tracking-label uppercase'>Din låt</span>}
  </span>
);
