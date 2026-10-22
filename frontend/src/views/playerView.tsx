import { Navigate } from 'react-router';
import { PlayerLobby } from '../features/lobby/components/playerLobby.tsx';
import { Rejoining } from '../features/lobby/components/rejoining.tsx';
import { usePlayerRejoin } from '../features/lobby/hooks/usePlayerRejoin.ts';
import { PlayerSubmitting } from '../features/submitting/components/playerSubmitting.tsx';
import { PhasePlaceholder } from '../shared/components/phasePlaceholder.tsx';
import { useRoomGame } from '../store/gameStore.ts';
import { usePlayerStore } from '../store/playerStore.ts';

// Mobilens vy under spelet. Väljer fas utifrån serverns state.
export const PlayerView = () => {
  const session = usePlayerStore((state) => state.session);
  const lastRoomCode = usePlayerStore((state) => state.lastRoomCode);
  const phase = useRoomGame(session?.roomCode)?.phase ?? 'lobby';
  const rejoining = usePlayerRejoin(null);

  if (!session) {
    if (rejoining) return <Rejoining />;
    // Efter "Lämna", en borttagning eller ett stängt rum går mobilen tillbaka till formuläret.
    return <Navigate to={lastRoomCode ? `/join?room=${lastRoomCode}` : '/join'} replace />;
  }

  if (phase === 'lobby') return <PlayerLobby session={session} />;
  if (phase === 'submitting') return <PlayerSubmitting />;
  return <PhasePlaceholder screen='player' />;
};
