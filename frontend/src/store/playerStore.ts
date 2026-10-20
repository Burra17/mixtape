import { create } from 'zustand';
import type { PlayerSession } from '../services/hub/hubContract.ts';

type PlayerState = {
  session: PlayerSession | null;
  // Rummet mobilen senast var i, så att /join kan fylla i koden igen efter "Lämna" eller en borttagning.
  lastRoomCode: string | null;
  notice: string | null;
  joined: (session: PlayerSession) => void;
  left: () => void;
  removedFromRoom: () => void;
};

export const usePlayerStore = create<PlayerState>()((set) => ({
  session: null,
  lastRoomCode: null,
  notice: null,
  joined: (session) => set({ session, lastRoomCode: session.roomCode, notice: null }),
  left: () => set({ session: null, notice: null }),
  removedFromRoom: () => set({ session: null, notice: 'Du togs bort från rummet.' }),
}));
