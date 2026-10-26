import { unlockAudio } from '../../../services/audio/audioPlayer.ts';
import { startGame, updateSettings } from '../../../services/hub/connection.ts';
import { useHubCall } from '../../../shared/hooks/useHubCall.ts';
import { useHostStore } from '../../../store/hostStore.ts';

// Värdens inställningar och "Starta spel". Den nya bilden kommer med GameStateUpdated, så här hanteras bara felet.
// busy används inte, så att inställningarna går att klicka på medan ett tidigare val skickas, som förut.
export const useGameSetup = () => {
  const { error, run } = useHubCall();

  const choose = (rounds: number | null, clipSeconds: number | null) => {
    const { session } = useHostStore.getState();
    if (session) void run(() => updateSettings(session, rounds, clipSeconds));
  };

  // Ljudet låses upp först, medan webbläsaren fortfarande räknar anropet som en del av klicket.
  const start = () => {
    const { session } = useHostStore.getState();
    if (!session) return;
    unlockAudio();
    void run(() => startGame(session));
  };

  return { choose, start, error };
};
