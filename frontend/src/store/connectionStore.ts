import { create } from 'zustand';

export type HubStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

type ConnectionState = {
  status: HubStatus;
  lastPongAt: Date | null;
  setStatus: (status: HubStatus) => void;
  receivePong: () => void;
};

// Börjar som 'connecting', eftersom vyerna som visar statusen startar anslutningen direkt.
export const useConnectionStore = create<ConnectionState>()((set) => ({
  status: 'connecting',
  lastPongAt: null,
  setStatus: (status) => set({ status }),
  receivePong: () => set({ lastPongAt: new Date() }),
}));
