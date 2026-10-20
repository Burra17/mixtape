// Kontraktet mot backendens GameHub (serverMethods) och IGameClient (clientEvents). Ändras alltid i samma PR som backenden.
export const serverMethods = {
  ping: 'Ping',
  createRoom: 'CreateRoom',
  resumeRoom: 'ResumeRoom',
  getJoinAddresses: 'GetJoinAddresses',
} as const;

export const clientEvents = {
  pong: 'Pong',
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
