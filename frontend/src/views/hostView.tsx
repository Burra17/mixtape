import { HostLobby } from '../features/lobby/components/hostLobby.tsx';
import { HostStart } from '../features/lobby/components/hostStart.tsx';
import { useHostRoom } from '../features/lobby/hooks/useHostRoom.ts';
import { useHostStore } from '../store/hostStore.ts';

// Storskärmens vy. Väljer skärm utifrån var värden är i flödet, och senare utifrån spelets fas.
export const HostView = () => {
  const { create, resume, startOver } = useHostRoom();
  const screen = useHostStore((state) => state.screen);

  if (screen === 'lobby') return <HostLobby />;

  return <HostStart onCreate={() => void create()} onResume={resume} onStartOver={() => void startOver()} />;
};
