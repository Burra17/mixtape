import { create } from 'zustand';
import type { PlayerSession } from '../services/hub/hubContract.ts';
import { useConnectionStore } from './connectionStore.ts';

type PlayerState = {
  session: PlayerSession | null;
  // Anslutningens epoch när spelaren senast kopplades till rummet. En ny epoch betyder en ny anslutning som måste kopplas igen.
  sessionEpoch: number;
  // Rummet mobilen senast var i, så att /join kan fylla i koden igen efter "Lämna" eller en borttagning.
  lastRoomCode: string | null;
  notice: string | null;
  joined: (session: PlayerSession) => void;
  left: () => void;
  removedFromRoom: (roomCode?: string) => void;
  roomClosed: () => void;
};

export const usePlayerStore = create<PlayerState>()((set) => ({
  session: null,
  sessionEpoch: 0,
  lastRoomCode: null,
  notice: null,
  joined: (session) => set({ session, sessionEpoch: useConnectionStore.getState().epoch, lastRoomCode: session.roomCode, notice: null }),
  left: () => set({ session: null, notice: null }),
  // roomCode skickas med när mobilen fick beskedet först vid återanslutningen, efter en omladdning som tömt minnet.
  removedFromRoom: (roomCode) =>
    set((state) => ({ session: null, lastRoomCode: roomCode ?? state.lastRoomCode, notice: 'Du togs bort från rummet.' })),
  // Koden fylls inte i igen, eftersom rummet inte finns längre.
  roomClosed: () => set({ session: null, lastRoomCode: null, notice: 'Rummet har stängts.' }),
}));
