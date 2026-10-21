import { HostLobby } from '../features/lobby/components/hostLobby.tsx';
import { HostStart } from '../features/lobby/components/hostStart.tsx';
import { useHostRoom } from '../features/lobby/hooks/useHostRoom.ts';
import { PhasePlaceholder } from '../shared/components/phasePlaceholder.tsx';
import { useGameStore } from '../store/gameStore.ts';
import { useHostStore } from '../store/hostStore.ts';

// Storskärmens vy. Väljer skärm utifrån var värden är i flödet, och i rummet utifrån spelets fas.
export const HostView = () => {
  const { create, resume, startOver } = useHostRoom();
  const screen = useHostStore((state) => state.screen);
  const phase = useGameStore((state) => state.game?.phase ?? 'lobby');

  if (screen === 'lobby') return phase === 'lobby' ? <HostLobby /> : <PhasePlaceholder screen='host' />;

  return <HostStart onCreate={() => void create()} onResume={resume} onStartOver={() => void startOver()} />;
};
