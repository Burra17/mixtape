import { useEffect, useRef } from 'react';
import { checkRoom, createRoom, getJoinAddresses, resumeRoom, startConnection } from '../../../services/hub/connection.ts';
import type { HostSession, HubResponse } from '../../../services/hub/hubContract.ts';
import { clearHostSession, loadHostSession, saveHostSession } from '../../../services/storage/hostSessionStorage.ts';
import { useConnectionStore } from '../../../store/connectionStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';

const { show, setJoinAddresses, setError } = useHostStore.getState();

const FAILED = 'Något gick fel. Försök igen.';

const enterLobby = (response: HubResponse<HostSession>) => {
  if (!response.value) {
    setError(response.error?.message ?? FAILED);
    return;
  }

  saveHostSession(response.value);
  show('lobby', response.value);
};

// Tar reda på om det finns ett sparat rum att fortsätta med. Körs när anslutningen kommit upp första gången.
const checkSavedRoom = async () => {
  getJoinAddresses()
    .then(setJoinAddresses)
    .catch(() => setJoinAddresses([]));

  const saved = loadHostSession();
  if (!saved) {
    show('start', null);
    return;
  }

  const response = await checkRoom(saved).catch(() => null);
  if (response?.value) {
    show('resumable', response.value);
  } else {
    clearHostSession();
    show('start', null);
  }
};

// Sidan har redan fått sitt klick och ljudet är upplåst, så en återanslutning utan omladdning tar tillbaka rummet utan "Fortsätt".
const resumeSilently = async (session: HostSession) => {
  const response = await resumeRoom(session).catch(() => null);
  if (response?.error) {
    clearHostSession();
    show('start', null);
    setError(response.error.message);
  }
};

// Flödet för storskärmen före spelet: skapa ett rum, fortsätta med det sparade eller kasta det och börja om.
export const useHostRoom = () => {
  const epoch = useConnectionStore((state) => state.epoch);
  const handledEpoch = useRef(0);

  useEffect(() => {
    void startConnection();
  }, []);

  useEffect(() => {
    if (epoch === 0 || epoch === handledEpoch.current) return;
    handledEpoch.current = epoch;

    const { screen, session } = useHostStore.getState();
    if (screen === 'checking') void checkSavedRoom();
    else if (screen === 'lobby' && session) void resumeSilently(session);
  }, [epoch]);

  return {
    create: () => createRoom(null).then(enterLobby, () => setError(FAILED)),
    // Klicket tar tillbaka rummet och häver pausen, och det är också klicket som låser upp ljudet.
    resume: () => {
      const { session } = useHostStore.getState();
      if (session) void resumeRoom(session).then(enterLobby, () => setError(FAILED));
    },
    startOver: () => createRoom(useHostStore.getState().session).then(enterLobby, () => setError(FAILED)),
  };
};
