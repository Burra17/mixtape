import { HostLobby } from '../features/lobby/components/hostLobby.tsx';
import { HostStart } from '../features/lobby/components/hostStart.tsx';
import { useHostRoom } from '../features/lobby/hooks/useHostRoom.ts';
import { HostPlaying } from '../features/playing/components/hostPlaying.tsx';
import { HostRevealing } from '../features/revealing/components/hostRevealing.tsx';
import { HostSubmitting } from '../features/submitting/components/hostSubmitting.tsx';
import { HostVoting } from '../features/voting/components/hostVoting.tsx';
import { PhasePlaceholder } from '../shared/components/phasePlaceholder.tsx';
import { useRoomGame } from '../store/gameStore.ts';
import { useHostStore } from '../store/hostStore.ts';

// Storskärmens vy. Väljer skärm utifrån var värden är i flödet, och i rummet utifrån spelets fas.
export const HostView = () => {
  const { create, resume, startOver } = useHostRoom();
  const screen = useHostStore((state) => state.screen);
  const session = useHostStore((state) => state.session);
  const phase = useRoomGame(session?.roomCode)?.phase ?? 'lobby';

  if (screen === 'lobby') {
    if (phase === 'lobby') return <HostLobby />;
    if (phase === 'submitting') return <HostSubmitting />;
    if (phase === 'playing') return <HostPlaying />;
    if (phase === 'voting') return <HostVoting />;
    if (phase === 'revealing') return <HostRevealing />;
    return <PhasePlaceholder screen='host' />;
  }

  return <HostStart onCreate={() => void create()} onResume={resume} onStartOver={() => void startOver()} />;
};
