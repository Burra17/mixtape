import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router';
import { joinRoom, startConnection } from '../../../services/hub/connection.ts';
import { savePlayerSession } from '../../../services/storage/playerSessionStorage.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// Mobilen går med i ett rum och fortsätter till /play. Felet och väntan är bara formulärets eget state.
export const useJoinRoom = () => {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    void startConnection();
  }, []);

  const join = async (roomCode: string, name: string) => {
    setBusy(true);
    setError(null);

    const response = (await startConnection()) ? await joinRoom(roomCode, name).catch(() => null) : null;
    setBusy(false);

    if (!response?.value) {
      setError(response?.error?.message ?? 'Kunde inte nå servern. Försök igen.');
      return;
    }

    savePlayerSession(response.value);
    usePlayerStore.getState().joined(response.value);
    void navigate('/play');
  };

  return { join, error, busy };
};
