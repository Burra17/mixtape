import { useEffect, useEffectEvent } from 'react';
import { continueGame, endGame, pauseGame } from '../../services/hub/connection.ts';
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

  // "Ja, avsluta" på pauskortet. Det delar anropet med pausen, så att kortet alltid visar felet från det senaste klicket.
  const end = async () => {
    if (!session || busy) return;
    await run(() => endGame(session));
  };

  // useEffectEvent läser alltid den senaste toggle, så lyssnaren kopplas bara en gång.
  const onSpace = useEffectEvent(() => void toggle());

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.code !== 'Space' || event.repeat) return;
      if (event.target instanceof HTMLElement && event.target.closest('button, input, textarea, select')) return;
      event.preventDefault();
      onSpace();
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, []);

  return { isPausedByHost, toggle, end, busy, error };
};
