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
  vote: 'Vote',
  getBallot: 'GetBallot',
  playAgain: 'PlayAgain',
  pauseGame: 'PauseGame',
  continueGame: 'ContinueGame',
  endGame: 'EndGame',
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
  // Att spelaren röstat, men aldrig på vilket bidrag.
  hasVoted: boolean;
};

// SettingsState i Application/Rooms.
export type SettingsState = {
  rounds: number;
  clipSeconds: number;
  submittingSeconds: number;
};

// PauseReason i Domain/Rooms.
export type PauseReason = 'hostAway' | 'pausedByHost';

// GameState i Application/Rooms, som kommer med GameStateUpdated.
export type GameState = {
  roomCode: string;
  revision: number;
  phase: Phase;
  // Spelet står still, för att storskärmen är borta eller värden pausat.
  isPaused: boolean;
  // Varför spelet står still, eller null. hostAway vinner om båda gäller.
  pauseReason: PauseReason | null;
  // 0 i lobbyn.
  round: number;
  // Rundans prompt, från inskicket till avslöjandet. null i lobbyn och i gameOver.
  prompt: string | null;
  // Sant under inskicket i rundan efter en runda som hoppades över för att färre än 2 skickade in.
  skippedRound: boolean;
  // Klippet som spelas, eller null utanför uppspelningen.
  nowPlaying: NowPlaying | null;
  // Bidragen att rösta på, i uppspelningens ordning, eller null utanför röstningen.
  ballot: BallotEntry[] | null;
  // Bidragen med vem som valde dem och antal röster, i uppspelningens ordning, eller null utanför avslöjandet.
  reveal: RevealedEntry[] | null;
  // Alla spelare sorterade efter placering i avslöjandet och slutställningen, annars null. Vinnarna är de med place 1.
  standings: GameStanding[] | null;
  // Tiden som var kvar av fasen när bilden skapades, eller null i faser utan deadline. Oberoende av mobilens klocka.
  remainingMs: number | null;
  settings: SettingsState;
  roundOptions: number[];
  clipSecondsOptions: number[];
  submittingSecondsOptions: number[];
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

// BallotEntry i Application/Rooms. number är platsen i uppspelningen och det som Vote tar emot. Ingenting om vem som valde låten.
export type BallotEntry = {
  number: number;
  title: string;
  artist: string;
  coverUrl: string;
};

// Ballot i Application/Rooms, bara till spelaren själv. Numret på det egna bidraget och på bidraget spelaren röstat på, eller null.
export type Ballot = {
  ownEntry: number | null;
  votedEntry: number | null;
};

// RevealedEntry i Application/Rooms. Vem som valde låten lämnar servern först när röstningen är slut.
export type RevealedEntry = {
  number: number;
  title: string;
  artist: string;
  coverUrl: string;
  playerId: string;
  votes: number;
};

// GameStanding i Application/Rooms. score gäller hela spelet och roundPoints rundan. place delas vid lika poäng, som 1, 1, 3.
export type GameStanding = {
  playerId: string;
  score: number;
  roundPoints: number;
  place: number;
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
