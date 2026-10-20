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
  show: (screen: HostScreen, session?: HostSession | null) => void;
  setJoinAddresses: (addresses: string[]) => void;
  nextAddress: () => void;
  setError: (error: string | null) => void;
};

export const useHostStore = create<HostState>()((set) => ({
  screen: 'checking',
  session: null,
  joinAddresses: [],
  addressIndex: 0,
  error: null,
  show: (screen, session) => set((state) => ({ screen, session: session === undefined ? state.session : session, error: null })),
  setJoinAddresses: (joinAddresses) => set({ joinAddresses, addressIndex: 0 }),
  nextAddress: () => set((state) => ({ addressIndex: (state.addressIndex + 1) % Math.max(state.joinAddresses.length, 1) })),
  setError: (error) => set({ error }),
}));
