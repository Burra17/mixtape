import { useNavigate } from 'react-router';
import { joinRoom, startConnection } from '../../../services/hub/connection.ts';
import { savePlayerSession } from '../../../services/storage/playerSessionStorage.ts';
import { useHubCall } from '../../../shared/hooks/useHubCall.ts';
import { useConnectionStore } from '../../../store/connectionStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// Mobilen går med i ett rum och fortsätter till /play. Felet och väntan är bara formulärets eget state.
export const useJoinRoom = () => {
  const navigate = useNavigate();
  const { busy, error, run } = useHubCall();

  const join = async (roomCode: string, name: string) => {
    // Anslutningen startas inuti anropet, så att en anslutning som inte kommer upp visas som ett anrop utan svar.
    const response = await run(async () => ((await startConnection()) ? joinRoom(roomCode, name) : null));
    if (response.error) return;

    savePlayerSession(response.value);
    usePlayerStore.getState().joined(response.value, useConnectionStore.getState().epoch);
    void navigate('/play');
  };

  return { join, error, busy };
};
