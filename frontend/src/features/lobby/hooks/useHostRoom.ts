import { useEffect } from 'react';
import { createRoom, getJoinAddresses, resumeRoom, startConnection } from '../../../services/hub/connection.ts';
import type { HostSession, HubResponse } from '../../../services/hub/hubContract.ts';
import { clearHostSession, loadHostSession, saveHostSession } from '../../../services/storage/hostSessionStorage.ts';
import { useHostStore } from '../../../store/hostStore.ts';

const { show, setJoinAddresses, setError } = useHostStore.getState();

const enterLobby = (response: HubResponse<HostSession>) => {
  if (!response.value) {
    setError(response.error?.message ?? 'Något gick fel. Försök igen.');
    return;
  }

  saveHostSession(response.value);
  show('lobby', response.value);
};

// Ansluter storskärmen och tar reda på om det finns ett sparat rum att fortsätta med.
const checkSavedRoom = async () => {
  await startConnection();
  getJoinAddresses()
    .then(setJoinAddresses)
    .catch(() => setJoinAddresses([]));

  const saved = loadHostSession();
  if (!saved) {
    show('start', null);
    return;
  }

  const response = await resumeRoom(saved).catch(() => null);
  if (response?.value) {
    show('resumable', response.value);
  } else {
    clearHostSession();
    show('start', null);
  }
};

// Flödet för storskärmen före spelet: skapa ett rum, fortsätta med det sparade eller kasta det och börja om.
export const useHostRoom = () => {
  useEffect(() => {
    void checkSavedRoom();
  }, []);

  return {
    create: () => createRoom(null).then(enterLobby, () => setError('Kunde inte nå servern. Försök igen.')),
    resume: () => show('lobby'),
    startOver: () => createRoom(useHostStore.getState().session).then(enterLobby, () => setError('Kunde inte nå servern. Försök igen.')),
  };
};
