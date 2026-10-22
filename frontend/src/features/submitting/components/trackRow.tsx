import type { Track } from '../../../services/hub/hubContract.ts';

// En låt som omslag, titel och artist med albumet, och ett E för explicit text.
export const TrackRow = ({ track }: { track: Track }) => (
  <span className='flex min-w-0 items-center gap-3 text-left'>
    <img src={track.coverUrl} alt='' width={56} height={56} className='size-14 shrink-0 border-2 border-ink bg-paper' />
    <span className='flex min-w-0 flex-col'>
      <span className='flex items-center gap-2 font-extrabold'>
        <span className='truncate'>{track.title}</span>
        {track.isExplicit && (
          <span title='Explicit text' aria-label='Explicit text' className='shrink-0 bg-ink px-1 text-xs font-black text-paper'>
            E
          </span>
        )}
      </span>
      <span className='truncate text-muted'>
        {track.artist} · {track.album}
      </span>
    </span>
  </span>
);
