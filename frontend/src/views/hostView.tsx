import { HostGameOver } from '../features/gameOver/components/hostGameOver.tsx';
import { HostGuessing } from '../features/guessing/components/hostGuessing.tsx';
import { HostLobby } from '../features/lobby/components/hostLobby.tsx';
import { HostStart } from '../features/lobby/components/hostStart.tsx';
import { useHostRoom } from '../features/lobby/hooks/useHostRoom.ts';
import { HostPlaying } from '../features/playing/components/hostPlaying.tsx';
import { HostRevealing } from '../features/revealing/components/hostRevealing.tsx';
import { HostSongReveal } from '../features/songReveal/components/hostSongReveal.tsx';
import { HostSubmitting } from '../features/submitting/components/hostSubmitting.tsx';
import { HostVoting } from '../features/voting/components/hostVoting.tsx';
import type { Phase } from '../services/hub/hubContract.ts';
import { PauseControls } from '../shared/components/pauseControls.tsx';
import { useRoomGame } from '../store/gameStore.ts';
import { useHostStore } from '../store/hostStore.ts';

// Faserna med nedräkning, som är de enda som går att pausa.
const PAUSABLE: Phase[] = ['submitting', 'playing', 'voting', 'revealing', 'guessing', 'songReveal'];

// Storskärmens vy. Väljer skärm utifrån var värden är i flödet, och i rummet utifrån spelets fas.
export const HostView = () => {
  const { create, resume, startOver } = useHostRoom();
  const screen = useHostStore((state) => state.screen);
  const session = useHostStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const phase = game?.phase ?? 'lobby';

  if (screen === 'lobby' && PAUSABLE.includes(phase)) {
    // data-paused fryser all rörelse i fasen, som textbandet och skivan, se index.css.
    return (
      <div data-paused={game?.pauseReason === 'pausedByHost' || undefined}>
        {phase === 'submitting' && <HostSubmitting />}
        {phase === 'playing' && <HostPlaying />}
        {phase === 'voting' && <HostVoting />}
        {phase === 'revealing' && <HostRevealing />}
        {phase === 'guessing' && <HostGuessing />}
        {phase === 'songReveal' && <HostSongReveal />}
        <PauseControls />
      </div>
    );
  }

  if (screen === 'lobby') return phase === 'lobby' ? <HostLobby /> : <HostGameOver />;

  return <HostStart onCreate={() => void create()} onResume={resume} onStartOver={() => void startOver()} />;
};
