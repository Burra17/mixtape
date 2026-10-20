import { create } from 'zustand';
import type { LobbyState } from '../services/hub/hubContract.ts';

type LobbyStoreState = {
  lobby: LobbyState | null;
  receiveLobby: (lobby: LobbyState) => void;
};

// Serverns senaste bild av lobbyn, samma för storskärmen och mobilen.
export const useLobbyStore = create<LobbyStoreState>()((set) => ({
  lobby: null,
  // Bilderna kan komma i fel ordning, så en äldre bild av samma rum kastas.
  receiveLobby: (lobby) => set((state) => (state.lobby?.roomCode === lobby.roomCode && state.lobby.revision > lobby.revision ? state : { lobby })),
}));
