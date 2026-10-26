import { Button } from '../../../shared/components/button.tsx';
import { ErrorLine } from '../../../shared/components/errorLine.tsx';
import { findPlayer } from '../../../shared/components/findPlayer.ts';
import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { StandingsList } from '../../../shared/components/standingsList.tsx';
import { useIsConnected } from '../../../store/connectionStore.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';
import { usePlayAgain } from '../hooks/usePlayAgain.ts';

// "Alva", "Alva och Bea", "Alva, Bea och Cem".
const joinNames = (names: string[]) => (names.length < 2 ? names.join('') : `${names.slice(0, -1).join(', ')} och ${names.at(-1)}`);

// Storskärmen efter sista rundan: vinnaren stort, hela ställningen och "Spela igen".
export const HostGameOver = () => {
  const session = useHostStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const isConnected = useIsConnected();
  const { restart, error, busy } = usePlayAgain();

  if (!game?.standings) return null;

  const winners = game.standings
    .filter((standing) => standing.place === 1)
    .map((standing) => findPlayer(game.players, standing.playerId)?.name ?? '');

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text='Slutställning' />
      <main className='flex flex-1 flex-col justify-center gap-8 px-6 py-12 lg:px-[6vw]'>
        <Label>Slutställning</Label>
        {/* Radavståndet rymmer prickarna och ringen i Å, Ä och Ö även när rubriken bryts. */}
        <h1 className='text-[clamp(2.5rem,7vw,7rem)] text-display leading-[1.25] [overflow-wrap:anywhere]'>
          {winners.length === 1 ? `${winners[0]} vinner` : 'Delad seger'}
        </h1>
        {winners.length > 1 && <p className='text-[clamp(1.5rem,2.5vw,2.5rem)] font-extrabold'>{joinNames(winners)}</p>}
        <StandingsList standings={game.standings} players={game.players} showsRoundPoints={false} />
        <div className='flex flex-col items-start gap-3'>
          {error && <ErrorLine error={error} />}
          <Button variant='primary' onClick={() => void restart()} disabled={busy || !isConnected}>
            Spela igen
          </Button>
        </div>
      </main>
    </div>
  );
};
