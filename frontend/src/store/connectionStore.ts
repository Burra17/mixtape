import { create } from 'zustand';

export type HubStatus = 'connecting' | 'connected' | 'reconnecting';

type ConnectionState = {
  status: HubStatus;
  // När anslutningen försvann, så att raden kan byta till "Tappade anslutningen" efter en stund.
  lostSince: number | null;
  // Räknas upp varje gång anslutningen kommer upp. Hooksen lyssnar på den för att koppla spelaren eller storskärmen till rummet igen.
  epoch: number;
  connected: () => void;
  lost: () => void;
};

// Börjar som 'connecting', eftersom vyerna som behöver anslutningen startar den direkt.
export const useConnectionStore = create<ConnectionState>()((set) => ({
  status: 'connecting',
  lostSince: null,
  epoch: 0,
  connected: () => set((state) => ({ status: 'connected', lostSince: null, epoch: state.epoch + 1 })),
  lost: () => set((state) => ({ status: 'reconnecting', lostSince: state.lostSince ?? Date.now() })),
}));

// Knappar som anropar servern stängs av så länge anslutningen är borta.
export const useIsConnected = () => useConnectionStore((state) => state.status === 'connected');
