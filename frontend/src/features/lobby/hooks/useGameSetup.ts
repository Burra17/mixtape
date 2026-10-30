import { unlockAudio } from '../../../services/audio/audioPlayer.ts';
import { chooseMode, startGame, updateAuxCordSettings, updateDropTheNeedleSettings } from '../../../services/hub/connection.ts';
import type { GameMode, GameState, HostSession, HubResponse } from '../../../services/hub/hubContract.ts';
import { useHubCall } from '../../../shared/hooks/useHubCall.ts';
import { useHostStore } from '../../../store/hostStore.ts';

// Värdens val av läge, inställningar och "Starta spel". Den nya bilden kommer med GameStateUpdated, så här hanteras bara felet.
// busy används inte, så att inställningarna går att klicka på medan ett tidigare val skickas, som förut.
export const useGameSetup = () => {
  const { error, run } = useHubCall();

  const send = (call: (session: HostSession) => Promise<HubResponse<GameState>>) => {
    const { session } = useHostStore.getState();
    if (session) void run(() => call(session));
  };

  const pickMode = (mode: GameMode) => send((session) => chooseMode(session, mode));

  const chooseAuxCord = (rounds: number | null, clipSeconds: number | null, submittingSeconds: number | null) =>
    send((session) => updateAuxCordSettings(session, rounds, clipSeconds, submittingSeconds));

  const chooseDropTheNeedle = (songs: number | null, clipSeconds: number | null) =>
    send((session) => updateDropTheNeedleSettings(session, songs, clipSeconds));

  // Ljudet låses upp först, medan webbläsaren fortfarande räknar anropet som en del av klicket.
  const start = () => {
    unlockAudio();
    send(startGame);
  };

  return { pickMode, chooseAuxCord, chooseDropTheNeedle, start, error };
};
