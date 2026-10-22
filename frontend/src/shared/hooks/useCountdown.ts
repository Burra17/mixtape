import { useEffect, useState } from 'react';
import { useGameStore } from '../../store/gameStore.ts';

const toSeconds = (ms: number) => Math.ceil(Math.max(ms, 0) / 1000);

// Hela sekunder kvar, räknat från när bilden kom fram enligt enhetens egen klocka, så att en mobil med fel klocka ändå räknar rätt.
export const useCountdown = () => {
  const remainingMs = useGameStore((state) => state.game?.remainingMs ?? null);
  const isPaused = useGameStore((state) => state.game?.isPaused ?? false);
  const receivedAt = useGameStore((state) => state.receivedAt);
  const [now, setNow] = useState(() => Date.now());

  const isRunning = remainingMs !== null && !isPaused;
  useEffect(() => {
    if (!isRunning) return;
    const timer = setInterval(() => setNow(Date.now()), 250);
    return () => clearInterval(timer);
  }, [isRunning, receivedAt]);

  if (remainingMs === null) return null;
  if (isPaused) return toSeconds(remainingMs);
  // now kan vara äldre än bilden om klockan stod still, och då räknas från när bilden kom.
  return toSeconds(receivedAt + remainingMs - Math.max(now, receivedAt));
};
