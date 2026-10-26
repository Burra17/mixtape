import { playAgain } from '../../../services/hub/connection.ts';
import { useHubCall } from '../../../shared/hooks/useHubCall.ts';
import { useHostStore } from '../../../store/hostStore.ts';

// Värdens "Spela igen". Bilden av lobbyn kommer med GameStateUpdated, så här hanteras bara felet och väntan.
export const usePlayAgain = () => {
  const { busy, error, run } = useHubCall();

  const restart = async () => {
    const { session } = useHostStore.getState();
    if (session) await run(() => playAgain(session));
  };

  return { restart, error, busy };
};
