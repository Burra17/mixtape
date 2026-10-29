import { endGame } from '../../services/hub/connection.ts';
import { useHostStore } from '../../store/hostStore.ts';
import { useHubCall } from './useHubCall.ts';

// Värdens "Ja, avsluta" på pauskortet. Slutställningen kommer med GameStateUpdated, så här hanteras bara felet och väntan.
export const useEndGame = () => {
  const { busy, error, run } = useHubCall();

  const end = async () => {
    const { session } = useHostStore.getState();
    if (session) await run(() => endGame(session));
  };

  return { end, error, busy };
};
