import { Navigate } from 'react-router';
import { PlayerGameOver } from '../features/gameOver/components/playerGameOver.tsx';
import { PlayerLobby } from '../features/lobby/components/playerLobby.tsx';
import { Rejoining } from '../features/lobby/components/rejoining.tsx';
import { usePlayerRejoin } from '../features/lobby/hooks/usePlayerRejoin.ts';
import { PlayerPlaying } from '../features/playing/components/playerPlaying.tsx';
import { PlayerRevealing } from '../features/revealing/components/playerRevealing.tsx';
import { PlayerSubmitting } from '../features/submitting/components/playerSubmitting.tsx';
import { PlayerVoting } from '../features/voting/components/playerVoting.tsx';
import { useRoomGame } from '../store/gameStore.ts';
import { usePlayerStore } from '../store/playerStore.ts';

// Mobilens vy under spelet. Väljer fas utifrån serverns state.
export const PlayerView = () => {
  const session = usePlayerStore((state) => state.session);
  const lastRoomCode = usePlayerStore((state) => state.lastRoomCode);
  const game = useRoomGame(session?.roomCode);
  const phase = game?.phase ?? 'lobby';
  const rejoining = usePlayerRejoin(null);

  if (!session) {
    if (rejoining) return <Rejoining />;
    // Efter "Lämna", en borttagning eller ett stängt rum går mobilen tillbaka till formuläret.
    return <Navigate to={lastRoomCode ? `/join?room=${lastRoomCode}` : '/join'} replace />;
  }

  if (phase === 'lobby') return <PlayerLobby session={session} />;
  // Nyckeln är rundan, så att sökningen börjar om när en överhoppad runda går direkt till nästa inskick.
  if (phase === 'submitting') return <PlayerSubmitting key={game?.round} />;
  if (phase === 'playing') return <PlayerPlaying />;
  // Nyckeln är rundan, så att röstsedeln och markeringen börjar om i varje runda.
  if (phase === 'voting') return <PlayerVoting key={game?.round} />;
  if (phase === 'revealing') return <PlayerRevealing />;
  return <PlayerGameOver />;
};
