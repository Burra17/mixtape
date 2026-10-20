import { leaveRoom } from '../../../services/hub/connection.ts';
import type { PlayerSession } from '../../../services/hub/hubContract.ts';
import { clearPlayerSession } from '../../../services/storage/playerSessionStorage.ts';
import { useLobbyStore } from '../../../store/lobbyStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// Mobilens bild av lobbyn: spelaren själv, hur många som är med och "Lämna".
export const usePlayerLobby = (session: PlayerSession) => {
  const lobby = useLobbyStore((state) => state.lobby);
  const current = lobby?.roomCode === session.roomCode ? lobby : null;

  const leave = async () => {
    await leaveRoom(session);
    clearPlayerSession();
    usePlayerStore.getState().left();
  };

  return {
    lobby: current,
    me: current?.players.find((player) => player.id === session.playerId) ?? null,
    leave,
  };
};
