import { leaveRoom } from '../../../services/hub/connection.ts';
import type { PlayerSession } from '../../../services/hub/hubContract.ts';
import { clearPlayerSession } from '../../../services/storage/playerSessionStorage.ts';
import { useGameStore } from '../../../store/gameStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// Mobilens bild av lobbyn: spelaren själv, hur många som är med och "Lämna".
export const usePlayerLobby = (session: PlayerSession) => {
  const lobby = useGameStore((state) => state.game);
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
