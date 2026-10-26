import type { GamePlayer, GameStanding } from '../../services/hub/hubContract.ts';
import { findPlayer } from './findPlayer.ts';
import { PlayerChip } from './playerChip.tsx';

type StandingsListProps = {
  standings: GameStanding[];
  players: GamePlayer[];
  // Avslöjandet visar vad rundan gav. Slutställningen visar bara den totala poängen.
  showsRoundPoints: boolean;
};

// Storskärmens ställning: placeringen, spelaren och den totala poängen.
export const StandingsList = ({ standings, players, showsRoundPoints }: StandingsListProps) => (
  <ol aria-label='Ställningen' className='flex flex-col gap-4 text-[clamp(1.25rem,2vw,2rem)]'>
    {standings.map((standing) => {
      const player = findPlayer(players, standing.playerId);
      return (
        <li key={standing.playerId} className='flex items-center gap-6'>
          <span className='w-[2em] shrink-0 font-black tabular-nums'>{standing.place}.</span>
          <span className='min-w-0 flex-1'>{player && <PlayerChip player={player} />}</span>
          <span className='shrink-0 font-black tabular-nums'>{standing.score} p</span>
          {showsRoundPoints && <span className='w-[3em] shrink-0 text-right font-bold text-muted tabular-nums'>+{standing.roundPoints}</span>}
        </li>
      );
    })}
  </ol>
);
