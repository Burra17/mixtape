import type { GamePlayer, RevealedEntry } from '../../../services/hub/hubContract.ts';
import { PlayerChip } from '../../../shared/components/playerChip.tsx';

type RevealListProps = {
  reveal: RevealedEntry[];
  players: GamePlayer[];
};

const votesText = (votes: number) => (votes === 1 ? '1 röst' : `${votes} röster`);

// Storskärmens första steg i avslöjandet: varje bidrag med vem som valde det och hur många röster det fick.
export const RevealList = ({ reveal, players }: RevealListProps) => (
  <ol aria-label='Vem som valde vad' className='flex flex-col gap-5 text-[clamp(1.1rem,1.6vw,1.6rem)]'>
    {reveal.map((entry) => {
      const owner = players.find((player) => player.id === entry.playerId);
      return (
        <li key={entry.number} className='flex items-center gap-5'>
          <span className='w-[1.5em] shrink-0 font-black tabular-nums'>{entry.number}</span>
          <img src={entry.coverUrl} alt='' className='size-[3.5em] shrink-0 border-2 border-ink bg-card shadow-hard' />
          <span className='flex min-w-0 flex-1 flex-col'>
            <span className='truncate font-extrabold'>{entry.title}</span>
            <span className='truncate text-muted'>{entry.artist}</span>
          </span>
          {owner && <PlayerChip player={owner} />}
          <span className='w-[5em] shrink-0 text-right font-black tabular-nums'>{votesText(entry.votes)}</span>
        </li>
      );
    })}
  </ol>
);
