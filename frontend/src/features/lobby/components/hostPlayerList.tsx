import { removePlayer } from '../../../services/hub/connection.ts';
import { Button } from '../../../shared/components/button.tsx';
import { Card } from '../../../shared/components/card.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { useHostStore } from '../../../store/hostStore.ts';
import { useLobbyStore } from '../../../store/lobbyStore.ts';
import { PlayerChip } from './playerChip.tsx';

// Spelarna i realtid. Ett klick på ett namn tar bort spelaren, och "Starta" gör inget förrän spelet finns.
export const HostPlayerList = () => {
  const session = useHostStore((state) => state.session);
  const lobby = useLobbyStore((state) => state.lobby);

  if (!session || lobby?.roomCode !== session.roomCode) return null;

  return (
    <Card>
      <Label>
        Spelare {lobby.players.length} av {lobby.maxPlayers}
      </Label>
      {lobby.players.length === 0 ? (
        <p className='text-[clamp(1.25rem,1.8vw,1.75rem)] font-medium text-muted'>Ingen har gått med än.</p>
      ) : (
        <ul className='grid gap-3 text-[clamp(1.25rem,1.8vw,1.75rem)] sm:grid-cols-2'>
          {lobby.players.map((player) => (
            <li key={player.id}>
              <button
                type='button'
                onClick={() => void removePlayer(session, player.id)}
                aria-label={`Ta bort ${player.name}`}
                title='Ta bort'
                className='w-full text-left decoration-2 hover:line-through focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink'
              >
                <PlayerChip player={player} />
              </button>
            </li>
          ))}
        </ul>
      )}
      <p className='font-medium text-muted'>
        {lobby.players.length > 0 && 'Klicka på ett namn för att ta bort spelaren. '}
        {!lobby.canStart && 'Minst 3 spelare behövs.'}
      </p>
      <div>
        <Button variant='primary' disabled={!lobby.canStart}>
          Starta
        </Button>
      </div>
    </Card>
  );
};
