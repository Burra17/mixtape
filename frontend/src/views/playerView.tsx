import { Navigate } from 'react-router';
import { PlayerLobby } from '../features/lobby/components/playerLobby.tsx';
import { Rejoining } from '../features/lobby/components/rejoining.tsx';
import { usePlayerRejoin } from '../features/lobby/hooks/usePlayerRejoin.ts';
import { usePlayerStore } from '../store/playerStore.ts';

// Mobilens vy under spelet. Väljer fas utifrån serverns state när fler faser finns.
export const PlayerView = () => {
  const session = usePlayerStore((state) => state.session);
  const lastRoomCode = usePlayerStore((state) => state.lastRoomCode);
  const rejoining = usePlayerRejoin(null);

  if (!session) {
    if (rejoining) return <Rejoining />;
    // Efter "Lämna", en borttagning eller ett stängt rum går mobilen tillbaka till formuläret.
    return <Navigate to={lastRoomCode ? `/join?room=${lastRoomCode}` : '/join'} replace />;
  }

  return <PlayerLobby session={session} />;
};
