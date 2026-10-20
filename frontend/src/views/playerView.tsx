import { useEffect } from 'react';
import { Navigate } from 'react-router';
import { PlayerLobby } from '../features/lobby/components/playerLobby.tsx';
import { startConnection } from '../services/hub/connection.ts';
import { usePlayerStore } from '../store/playerStore.ts';

// Mobilens vy under spelet. Väljer fas utifrån serverns state när fler faser finns.
export const PlayerView = () => {
  const session = usePlayerStore((state) => state.session);
  const lastRoomCode = usePlayerStore((state) => state.lastRoomCode);

  useEffect(() => {
    void startConnection();
  }, []);

  // Utan session, efter "Lämna", en borttagning eller en omladdning, går mobilen tillbaka till formuläret.
  if (!session) return <Navigate to={lastRoomCode ? `/join?room=${lastRoomCode}` : '/join'} replace />;

  return <PlayerLobby session={session} />;
};
