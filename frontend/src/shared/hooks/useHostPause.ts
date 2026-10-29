import { useEffect } from 'react';
import { continueGame, pauseGame } from '../../services/hub/connection.ts';
import { useRoomGame } from '../../store/gameStore.ts';
import { useHostStore } from '../../store/hostStore.ts';
import { useHubCall } from './useHubCall.ts';

// Värdens paus. Mellanslag växlar mellan paus och fortsätt, utom när en knapp eller ett fält har fokus, eftersom mellanslag då klickar på det.
export const useHostPause = () => {
  const session = useHostStore((state) => state.session);
  const isPausedByHost = useRoomGame(session?.roomCode)?.pauseReason === 'pausedByHost';
  const { busy, error, run } = useHubCall();

  const toggle = async () => {
    if (!session || busy) return;
    await run(() => (isPausedByHost ? continueGame(session) : pauseGame(session)));
  };

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.code !== 'Space' || event.repeat) return;
      if (event.target instanceof HTMLElement && event.target.closest('button, input, textarea, select')) return;
      event.preventDefault();
      void toggle();
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  });

  return { isPausedByHost, toggle, busy, error };
};
