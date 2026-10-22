import type { GamePlayer } from '../../../services/hub/hubContract.ts';
import { playerBackground } from './playerColors.ts';

const graphemes = new Intl.Segmenter('sv', { granularity: 'grapheme' });

// Första tecknet som det syns, så att en emoji inte delas på mitten.
const initial = (name: string) => (graphemes.segment(name)[Symbol.iterator]().next().value?.segment ?? '').toLocaleUpperCase('sv');

// En spelare som färgruta med initialen och namnet bredvid, så att ingen behöver skilja spelarna åt bara på färgen.
export const PlayerChip = ({ player }: { player: GamePlayer }) => (
  <span className='flex items-center gap-3'>
    <span
      aria-hidden='true'
      className={`flex size-[1.6em] shrink-0 items-center justify-center font-black text-white ${playerBackground(player.colorIndex)}`}
    >
      {initial(player.name)}
    </span>
    <span className='font-extrabold [overflow-wrap:anywhere]'>{player.name}</span>
  </span>
);
