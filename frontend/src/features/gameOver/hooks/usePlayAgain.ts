import { useState } from 'react';
import { playAgain, UNREACHABLE } from '../../../services/hub/connection.ts';
import { useHostStore } from '../../../store/hostStore.ts';

// Värdens "Spela igen". Bilden av lobbyn kommer med GameStateUpdated, så här hanteras bara felet och väntan.
export const usePlayAgain = () => {
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const restart = async () => {
    const { session } = useHostStore.getState();
    if (!session) return;
    setBusy(true);
    setError(null);
    const response = await playAgain(session).catch(() => null);
    setBusy(false);
    if (!response?.value) setError(response?.error?.message ?? UNREACHABLE);
  };

  return { restart, error, busy };
};
