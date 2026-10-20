// Kontraktet mot backendens GameHub (serverMethods) och IGameClient (clientEvents). Ändras alltid i samma PR som backenden.
export const serverMethods = {
  ping: 'Ping',
  createRoom: 'CreateRoom',
  checkRoom: 'CheckRoom',
  resumeRoom: 'ResumeRoom',
  getJoinAddresses: 'GetJoinAddresses',
  joinRoom: 'JoinRoom',
  rejoinRoom: 'RejoinRoom',
  leaveRoom: 'LeaveRoom',
  removePlayer: 'RemovePlayer',
  search: 'Search',
} as const;

export const clientEvents = {
  pong: 'Pong',
  lobbyUpdated: 'LobbyUpdated',
  removedFromRoom: 'RemovedFromRoom',
  roomClosed: 'RoomClosed',
} as const;

// Error i Domain/Common.
export type HubError = {
  code: string;
  message: string;
};

// HubResponse<T> i Api/Hubs. Antingen value eller error är satt.
export type HubResponse<T> = {
  value: T | null;
  error: HubError | null;
};

// HostSession i Application/Rooms.
export type HostSession = {
  roomCode: string;
  hostToken: string;
};

// PlayerSession i Application/Rooms.
export type PlayerSession = {
  roomCode: string;
  playerId: string;
  playerToken: string;
};

// LobbyPlayer i Application/Rooms. colorIndex är 1–8, en plats i paletten player-1 till player-8.
export type LobbyPlayer = {
  id: string;
  name: string;
  colorIndex: number;
  // Platsen behålls när anslutningen tappas, så en spelare kan finnas kvar utan att vara nåbar.
  isConnected: boolean;
};

// LobbyState i Application/Rooms, som kommer med LobbyUpdated.
export type LobbyState = {
  roomCode: string;
  revision: number;
  players: LobbyPlayer[];
  minPlayers: number;
  maxPlayers: number;
  canStart: boolean;
  // Storskärmen är borta och spelet står still.
  isPaused: boolean;
};

// Track i Application/Music. Ingen klipplänk, eftersom den slutar gälla efter 15 minuter och hämtas färsk inför uppspelningen.
export type Track = {
  id: number;
  title: string;
  artist: string;
  album: string;
  coverUrl: string;
  isExplicit: boolean;
};
