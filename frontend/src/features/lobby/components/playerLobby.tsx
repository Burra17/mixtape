import type { PlayerSession } from '../../../services/hub/hubContract.ts';
import { Button } from '../../../shared/components/button.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { useIsConnected } from '../../../store/connectionStore.ts';
import { usePlayerLobby } from '../hooks/usePlayerLobby.ts';
import { playerText } from './playerColors.ts';

// Mobilen i lobbyn: ditt namn stort i din färg, hur många som är med och "Lämna".
export const PlayerLobby = ({ session }: { session: PlayerSession }) => {
  const { lobby, me, leave } = usePlayerLobby(session);
  const isConnected = useIsConnected();

  return (
    <main className='flex min-h-dvh flex-col justify-between gap-12 px-6 py-12'>
      <div className='flex flex-col gap-6'>
        <Label>Rum {session.roomCode}</Label>
        {me && lobby ? (
          <>
            <h1 className={`text-5xl text-display [overflow-wrap:anywhere] ${playerText(me.colorIndex)}`}>{me.name}</h1>
            <p className='text-2xl font-extrabold'>
              {lobby.players.length} av {lobby.maxPlayers} spelare
            </p>
            <p className='text-lg font-medium text-muted'>Väntar på att spelet startar</p>
            {lobby.isPaused && (
              <p role='status' className='self-start border-2 border-ink px-3 py-2 font-bold'>
                Storskärmen är borta. Spelet väntar.
              </p>
            )}
          </>
        ) : (
          <p className='text-2xl font-bold'>Hämtar lobbyn…</p>
        )}
      </div>
      <div>
        <Button onClick={() => void leave()} disabled={!isConnected}>
          Lämna
        </Button>
      </div>
    </main>
  );
};
