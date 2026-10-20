import { useEffect, useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router';
import { rejoinRoom, startConnection } from '../../../services/hub/connection.ts';
import type { PlayerSession } from '../../../services/hub/hubContract.ts';
import { clearPlayerSession, loadPlayerSession, savePlayerSession } from '../../../services/storage/playerSessionStorage.ts';
import { useConnectionStore } from '../../../store/connectionStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// En sparad session som hör till ett annat rum än QR-koden pekar på räknas inte. Då gäller koden.
const sessionFor = (roomFromLink: string | null) => {
  const session = usePlayerStore.getState().session ?? loadPlayerSession();
  return session && roomFromLink && roomFromLink !== session.roomCode ? null : session;
};

const rejoin = async (session: PlayerSession) => {
  const response = await rejoinRoom(session).catch(() => null);
  if (response?.value) {
    savePlayerSession(response.value);
    usePlayerStore.getState().joined(response.value);
    return true;
  }

  // Utan svar är anslutningen borta igen, och nästa epoch försöker på nytt.
  if (response?.error) {
    clearPlayerSession();
    const { roomClosed, removedFromRoom } = usePlayerStore.getState();
    if (response.error.code === 'room.not_found') roomClosed();
    else removedFromRoom(session.roomCode);
  }

  return false;
};

// Formuläret ska visa rummet mobilen togs bort från, och inget rum alls om det har stängts.
const joinPath = () => {
  const { lastRoomCode } = usePlayerStore.getState();
  return lastRoomCode ? `/join?room=${lastRoomCode}` : '/join';
};

// Kopplar mobilen till sin plats igen varje gång anslutningen kommer upp: vid sidladdning, efter en återanslutning och när sidan blir synlig.
// Svarar om ett försök pågår, så att vyn kan vänta med formuläret.
export const usePlayerRejoin = (roomFromLink: string | null) => {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const epoch = useConnectionStore((state) => state.epoch);
  const handledEpoch = useRef(0);
  // Bara en sparad session som inte redan finns i minnet behöver vänta på servern, till exempel efter en omladdning.
  const [rejoining, setRejoining] = useState(() => !usePlayerStore.getState().session && sessionFor(roomFromLink) !== null);

  useEffect(() => {
    void startConnection();
  }, []);

  useEffect(() => {
    // Bara en ny anslutning ska utlösa ett försök, inte en ny länk eller sökväg.
    if (epoch === 0 || epoch === handledEpoch.current) return;
    handledEpoch.current = epoch;

    const { session, sessionEpoch } = usePlayerStore.getState();
    // Spelaren gick med på den här anslutningen, så platsen är redan kopplad.
    if (session && sessionEpoch === epoch) return;

    const saved = sessionFor(roomFromLink);
    if (!saved) {
      // Den gamla platsen släpps när mobilen skannat koden till ett annat rum.
      if (roomFromLink) clearPlayerSession();
      return;
    }

    void rejoin(saved).then((ok) => {
      setRejoining(false);
      if (ok && pathname !== '/play') void navigate('/play');
      else if (!ok && !usePlayerStore.getState().session && pathname === '/join') void navigate(joinPath(), { replace: true });
    });
  }, [epoch, roomFromLink, pathname, navigate]);

  return rejoining;
};
