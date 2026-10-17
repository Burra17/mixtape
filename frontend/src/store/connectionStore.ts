import { create } from 'zustand';

export type ConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

type ConnectionState = {
  status: ConnectionStatus;
  lastPongAt: Date | null;
  setStatus: (status: ConnectionStatus) => void;
  receivePong: () => void;
};

export const useConnectionStore = create<ConnectionState>()((set) => ({
  status: 'disconnected',
  lastPongAt: null,
  setStatus: (status) => set({ status }),
  receivePong: () => set({ lastPongAt: new Date() }),
}));
