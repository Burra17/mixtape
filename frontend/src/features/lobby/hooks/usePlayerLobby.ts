import { leaveRoom } from '../../../services/hub/connection.ts';
import type { PlayerSession } from '../../../services/hub/hubContract.ts';
import { clearPlayerSession } from '../../../services/storage/playerSessionStorage.ts';
import { findPlayer } from '../../../shared/components/findPlayer.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// Mobilens bild av lobbyn: spelaren själv, hur många som är med och "Lämna".
export const usePlayerLobby = (session: PlayerSession) => {
  const game = useRoomGame(session.roomCode);

  const leave = async () => {
    await leaveRoom(session);
    clearPlayerSession();
    usePlayerStore.getState().left();
  };

  return {
    game,
    me: findPlayer(game?.players ?? [], session.playerId) ?? null,
    leave,
  };
};
