import type { GamePlayer, GameStanding } from '../../../services/hub/hubContract.ts';
import { findPlayer } from '../../../shared/components/findPlayer.ts';
import { PlayerChip } from '../../../shared/components/playerChip.tsx';

type StandingsListProps = {
  standings: GameStanding[];
  players: GamePlayer[];
};

// Storskärmens andra steg i avslöjandet: placeringen, den totala poängen och vad rundan gav.
export const StandingsList = ({ standings, players }: StandingsListProps) => (
  <ol aria-label='Ställningen' className='flex flex-col gap-4 text-[clamp(1.25rem,2vw,2rem)]'>
    {standings.map((standing) => {
      const player = findPlayer(players, standing.playerId);
      return (
        <li key={standing.playerId} className='flex items-center gap-6'>
          <span className='w-[2em] shrink-0 font-black tabular-nums'>{standing.place}.</span>
          <span className='min-w-0 flex-1'>{player && <PlayerChip player={player} />}</span>
          <span className='shrink-0 font-black tabular-nums'>{standing.score} p</span>
          <span className='w-[3em] shrink-0 text-right font-bold text-muted tabular-nums'>+{standing.roundPoints}</span>
        </li>
      );
    })}
  </ol>
);
