import { create } from 'zustand';
import type { HostSession } from '../services/hub/hubContract.ts';

// Vad storskärmen visar innan spelet har börjat. 'checking' medan en sparad session prövas mot servern.
export type HostScreen = 'checking' | 'start' | 'resumable' | 'lobby';

type HostState = {
  screen: HostScreen;
  session: HostSession | null;
  joinAddresses: string[];
  addressIndex: number;
  error: string | null;
  // Det sparade rummet fanns inte längre, till exempel efter städningen eller en omstart. Visas ovanför "Skapa spel".
  roomClosed: boolean;
  show: (screen: HostScreen, session?: HostSession | null) => void;
  setJoinAddresses: (addresses: string[]) => void;
  nextAddress: () => void;
  setError: (error: string | null) => void;
  showRoomClosed: () => void;
};

export const useHostStore = create<HostState>()((set) => ({
  screen: 'checking',
  session: null,
  joinAddresses: [],
  addressIndex: 0,
  error: null,
  roomClosed: false,
  show: (screen, session) => set((state) => ({ screen, session: session === undefined ? state.session : session, error: null, roomClosed: false })),
  setJoinAddresses: (joinAddresses) => set({ joinAddresses, addressIndex: 0 }),
  nextAddress: () => set((state) => ({ addressIndex: (state.addressIndex + 1) % Math.max(state.joinAddresses.length, 1) })),
  setError: (error) => set({ error }),
  showRoomClosed: () => set({ screen: 'start', session: null, error: null, roomClosed: true }),
}));
