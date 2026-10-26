import type { GamePlayer } from '../../services/hub/hubContract.ts';
import { PlayerChip } from './playerChip.tsx';

type PlayerChecklistProps = {
  players: GamePlayer[];
  label: string;
  isDone: (player: GamePlayer) => boolean;
};

// Storskärmens lista över vilka som är klara i fasen, men aldrig med vad.
export const PlayerChecklist = ({ players, label, isDone }: PlayerChecklistProps) => (
  <ul aria-label={label} className='flex flex-wrap gap-x-10 gap-y-4 text-[clamp(1.25rem,1.8vw,1.75rem)]'>
    {players.map((player) => (
      <li key={player.id} className='flex items-center gap-3'>
        {/* Nedtoningen syns dåligt på avstånd, så bocken och ordet "borta" står bredvid. */}
        <span className={isDone(player) ? '' : 'opacity-40'}>
          <PlayerChip player={player} />
        </span>
        {isDone(player) && (
          <span aria-label='klar' className='font-black'>
            ✓
          </span>
        )}
        {!player.isConnected && <span className='text-[0.7em] font-bold tracking-label text-muted uppercase'>borta</span>}
      </li>
    ))}
  </ul>
);
