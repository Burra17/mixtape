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
  updateSettings: 'UpdateSettings',
  startGame: 'StartGame',
  search: 'Search',
  submitTrack: 'SubmitTrack',
  getClip: 'GetClip',
} as const;

export const clientEvents = {
  pong: 'Pong',
  gameStateUpdated: 'GameStateUpdated',
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

// GamePhase i Domain/Rooms. Servern skickar enums som text i camelCase.
export const PHASES = ['lobby', 'submitting', 'playing', 'voting', 'revealing', 'gameOver'] as const;
export type Phase = (typeof PHASES)[number];

// GamePlayer i Application/Rooms. colorIndex är 1–8, en plats i paletten player-1 till player-8.
export type GamePlayer = {
  id: string;
  name: string;
  colorIndex: number;
  // Platsen behålls när anslutningen tappas, så en spelare kan finnas kvar utan att vara nåbar.
  isConnected: boolean;
  // Att spelaren skickat in, men aldrig vilken låt. Den stannar på servern till avslöjandet.
  hasSubmitted: boolean;
};

// SettingsState i Application/Rooms.
export type SettingsState = {
  rounds: number;
  clipSeconds: number;
};

// GameState i Application/Rooms, som kommer med GameStateUpdated.
export type GameState = {
  roomCode: string;
  revision: number;
  phase: Phase;
  // Storskärmen är borta och spelet står still.
  isPaused: boolean;
  // 0 i lobbyn.
  round: number;
  // Rundans prompt, från inskicket till avslöjandet. null i lobbyn och i gameOver.
  prompt: string | null;
  // Sant under inskicket i rundan efter en runda som hoppades över för att färre än 2 skickade in.
  skippedRound: boolean;
  // Klippet som spelas, eller null utanför uppspelningen.
  nowPlaying: NowPlaying | null;
  // Tiden som var kvar av fasen när bilden skapades, eller null i faser utan deadline. Oberoende av mobilens klocka.
  remainingMs: number | null;
  settings: SettingsState;
  roundOptions: number[];
  clipSecondsOptions: number[];
  players: GamePlayer[];
  minPlayers: number;
  maxPlayers: number;
  canStart: boolean;
};

// NowPlaying i Application/Rooms. Ingen länk och ingenting om vem som valde låten. index är 1 för det första klippet.
export type NowPlaying = {
  index: number;
  count: number;
  title: string;
  artist: string;
  album: string;
  coverUrl: string;
};

// Clip i Application/Rooms, bara till storskärmen. index visar vilket klipp länken gäller.
export type Clip = {
  index: number;
  previewUrl: string;
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
