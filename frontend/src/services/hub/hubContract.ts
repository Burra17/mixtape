// Kontraktet mot backendens GameHub (serverMethods) och IGameClient (clientEvents). Ändras alltid i samma PR som backenden.
export const serverMethods = {
  ping: 'Ping',
  createRoom: 'CreateRoom',
  resumeRoom: 'ResumeRoom',
  getJoinAddresses: 'GetJoinAddresses',
  joinRoom: 'JoinRoom',
  leaveRoom: 'LeaveRoom',
  removePlayer: 'RemovePlayer',
} as const;

export const clientEvents = {
  pong: 'Pong',
  lobbyUpdated: 'LobbyUpdated',
  removedFromRoom: 'RemovedFromRoom',
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
};

// LobbyState i Application/Rooms, som kommer med LobbyUpdated.
export type LobbyState = {
  roomCode: string;
  revision: number;
  players: LobbyPlayer[];
  minPlayers: number;
  maxPlayers: number;
  canStart: boolean;
};
