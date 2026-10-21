import { removePlayer } from '../../../services/hub/connection.ts';
import { Card } from '../../../shared/components/card.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { useIsConnected } from '../../../store/connectionStore.ts';
import { useGameStore } from '../../../store/gameStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';
import { PlayerChip } from './playerChip.tsx';

// Spelarna i realtid. Ett klick på ett namn tar bort spelaren.
export const HostPlayerList = () => {
  const session = useHostStore((state) => state.session);
  const lobby = useGameStore((state) => state.game);
  const isConnected = useIsConnected();

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
                disabled={!isConnected}
                aria-label={`Ta bort ${player.name}${player.isConnected ? '' : ', borta'}`}
                title='Ta bort'
                className='flex w-full items-center gap-3 text-left decoration-2 hover:line-through focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink disabled:cursor-not-allowed'
              >
                {/* Nedtoningen syns dåligt på avstånd, så ordet "borta" står bredvid. */}
                <span className={player.isConnected ? '' : 'opacity-40'}>
                  <PlayerChip player={player} />
                </span>
                {!player.isConnected && <span className='text-[0.7em] font-bold tracking-label text-muted uppercase'>borta</span>}
              </button>
            </li>
          ))}
        </ul>
      )}
      {lobby.players.length > 0 && <p className='font-medium text-muted'>Klicka på ett namn för att ta bort spelaren.</p>}
    </Card>
  );
};
