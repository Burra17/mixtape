import { useState } from 'react';
import { unlockAudio } from '../../../services/audio/audioPlayer.ts';
import { startGame, updateSettings } from '../../../services/hub/connection.ts';
import type { GameState, HubResponse } from '../../../services/hub/hubContract.ts';
import { useHostStore } from '../../../store/hostStore.ts';

const FAILED = 'Något gick fel. Försök igen.';

// Värdens inställningar och "Starta spel". Den nya bilden kommer med GameStateUpdated, så här hanteras bara felet.
export const useGameSetup = () => {
  const [error, setError] = useState<string | null>(null);

  const send = async (call: () => Promise<HubResponse<GameState>>) => {
    setError(null);
    const response = await call().catch(() => null);
    if (!response?.value) setError(response?.error?.message ?? FAILED);
  };

  const choose = (rounds: number, clipSeconds: number) => {
    const { session } = useHostStore.getState();
    if (session) void send(() => updateSettings(session, rounds, clipSeconds));
  };

  // Ljudet låses upp först, medan webbläsaren fortfarande räknar anropet som en del av klicket.
  const start = () => {
    const { session } = useHostStore.getState();
    if (!session) return;
    unlockAudio();
    void send(() => startGame(session));
  };

  return { choose, start, error };
};
