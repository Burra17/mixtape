import { useEffect, useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router';
import { leaveRoom, rejoinRoom, startConnection } from '../../../services/hub/connection.ts';
import type { PlayerSession } from '../../../services/hub/hubContract.ts';
import { clearPlayerSession, loadPlayerSession, savePlayerSession } from '../../../services/storage/playerSessionStorage.ts';
import { useConnectionStore } from '../../../store/connectionStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

type RejoinOutcome = 'rejoined' | 'refused' | 'offline';

const currentSession = () => usePlayerStore.getState().session ?? loadPlayerSession();

// En sparad session som hör till ett annat rum än QR-koden pekar på räknas inte. Då gäller koden.
const isOtherRoom = (session: PlayerSession, roomFromLink: string | null) => roomFromLink !== null && roomFromLink !== session.roomCode;

const rejoin = async (session: PlayerSession, epoch: number): Promise<RejoinOutcome> => {
  const response = await rejoinRoom(session).catch(() => null);
  if (response?.value) {
    savePlayerSession(response.value);
    usePlayerStore.getState().joined(response.value, epoch);
    return 'rejoined';
  }

  // Utan svar är anslutningen borta igen, och nästa epoch försöker på nytt.
  if (!response?.error) return 'offline';

  clearPlayerSession();
  const { roomClosed, removedFromRoom } = usePlayerStore.getState();
  if (response.error.code === 'player.not_found') removedFromRoom(session.roomCode);
  else roomClosed();
  return 'refused';
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
  const [rejoining, setRejoining] = useState(() => {
    const session = loadPlayerSession();
    return !usePlayerStore.getState().session && session !== null && !isOtherRoom(session, roomFromLink);
  });

  useEffect(() => {
    void startConnection();
  }, []);

  useEffect(() => {
    // Bara en ny anslutning ska utlösa ett försök, inte en ny länk eller sökväg. Två försök kan överlappa om anslutningen
    // byts mitt i, och då kopplar det senare den nya anslutningen. Navigeringen ersätter historiken, så den gör ingen skada två gånger.
    if (epoch === 0 || epoch === handledEpoch.current) return;
    handledEpoch.current = epoch;

    const { session: inMemory, sessionEpoch, left } = usePlayerStore.getState();
    // Spelaren gick med på den här anslutningen, så platsen är redan kopplad.
    if (inMemory && sessionEpoch === epoch) return;

    const session = currentSession();
    if (!session) return;

    // Mobilen har skannat koden till ett annat rum. Den gamla platsen släpps, så att den inte ligger kvar som spöke.
    if (isOtherRoom(session, roomFromLink)) {
      void leaveRoom(session);
      clearPlayerSession();
      left();
      return;
    }

    void rejoin(session, epoch).then((outcome) => {
      setRejoining(false);
      if (outcome === 'rejoined' && pathname !== '/play') void navigate('/play', { replace: true });
      if (outcome === 'refused' && pathname === '/join') void navigate(joinPath(), { replace: true });
    });
  }, [epoch, roomFromLink, pathname, navigate]);

  return rejoining;
};
