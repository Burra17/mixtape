import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { useConnectionStore } from '../../store/connectionStore.ts';
import { useGameStore } from '../../store/gameStore.ts';
import { usePlayerStore } from '../../store/playerStore.ts';
import { clearPlayerSession } from '../storage/playerSessionStorage.ts';
import { clientEvents, serverMethods, type HostSession, type GameState, type HubResponse, type PlayerSession, type Track } from './hubContract.ts';

// Två snabba försök först, eftersom de flesta avbrott är ett kort hack i wifi. Sedan var 5:e sekund.
const FIRST_RETRY_DELAYS_MS = [0, 2000];
const RETRY_INTERVAL_MS = 5000;
// Så länge får en ping ta när sidan blir synlig innan anslutningen räknas som död.
const ALIVE_TIMEOUT_MS = 3000;

// Försöker igen för evigt. En mobil kan vara låst i flera minuter, och då ska den komma tillbaka av sig själv.
const nextRetryDelay = (previousRetryCount: number) => FIRST_RETRY_DELAYS_MS[previousRetryCount] ?? RETRY_INTERVAL_MS;

// En enda anslutning för hela appen. Adressen är relativ, så Vite skickar den vidare till backenden.
const connection = new HubConnectionBuilder()
  .withUrl('/gamehub')
  .withAutomaticReconnect({ nextRetryDelayInMilliseconds: ({ previousRetryCount }) => nextRetryDelay(previousRetryCount) })
  .configureLogging(LogLevel.Warning)
  .build();

const { connected, lost } = useConnectionStore.getState();

connection.onreconnecting(lost);
connection.onreconnected(connected);
connection.onclose(lost);
connection.on(clientEvents.pong, () => undefined);
connection.on(clientEvents.gameStateUpdated, (game: GameState) => useGameStore.getState().receiveGame(game));
// Den borttagna mobilen lämnar själv rummets grupp, eftersom servern inte vet vilken anslutning den har.
connection.on(clientEvents.removedFromRoom, () => {
  const { session, removedFromRoom } = usePlayerStore.getState();
  if (session) void leaveRoom(session);
  clearPlayerSession();
  removedFromRoom(session?.roomCode);
});
connection.on(clientEvents.roomClosed, () => {
  clearPlayerSession();
  usePlayerStore.getState().roomClosed();
});

let wanted = false;
let starting: Promise<boolean> | null = null;
let retryTimer: ReturnType<typeof setTimeout> | undefined;

// Startar anslutningen om den inte redan är igång och svarar om den är uppe. Den som anropar under en pågående start får samma löfte.
// withAutomaticReconnect gäller bara en anslutning som varit uppe, så en misslyckad start försöker igen här.
export const startConnection = () => {
  wanted = true;
  if (connection.state === HubConnectionState.Disconnected) {
    clearTimeout(retryTimer);
    starting = connection.start().then(
      () => {
        connected();
        return true;
      },
      () => {
        lost();
        retryTimer = setTimeout(() => void startConnection(), RETRY_INTERVAL_MS);
        return false;
      },
    );
  }

  if (connection.state === HubConnectionState.Connecting && starting) return starting;
  return Promise.resolve(connection.state === HubConnectionState.Connected);
};

// "Försök igen" och en sida som blir synlig igen väntar inte på nästa schemalagda försök.
export const retryConnection = async () => {
  if (connection.state !== HubConnectionState.Disconnected) {
    await connection.stop();
  }

  return startConnection();
};

// En anslutning som SignalR tror är uppe kan vara död efter en låst skärm. En ping som inte kommer tillbaka avslöjar det.
const isAlive = () =>
  Promise.race([
    connection.invoke(serverMethods.ping).then(
      () => true,
      () => false,
    ),
    new Promise<boolean>((resolve) => setTimeout(() => resolve(false), ALIVE_TIMEOUT_MS)),
  ]);

document.addEventListener('visibilitychange', () => {
  if (!wanted || document.visibilityState !== 'visible') return;
  if (connection.state !== HubConnectionState.Connected) {
    void retryConnection();
    return;
  }

  void isAlive().then((alive) => {
    if (!alive) void retryConnection();
  });
});

// previous skickas med vid "Nytt spel", så att servern tar bort det gamla rummet. Storen töms före ett nytt rum, se gameStore.
export const createRoom = (previous: HostSession | null) => {
  useGameStore.getState().reset();
  return connection.invoke<HubResponse<HostSession>>(serverMethods.createRoom, previous);
};

export const checkRoom = (session: HostSession) => connection.invoke<HubResponse<HostSession>>(serverMethods.checkRoom, session);

export const resumeRoom = (session: HostSession) => connection.invoke<HubResponse<HostSession>>(serverMethods.resumeRoom, session);

export const getJoinAddresses = () => connection.invoke<string[]>(serverMethods.getJoinAddresses);

export const joinRoom = (roomCode: string, name: string) => {
  useGameStore.getState().reset();
  return connection.invoke<HubResponse<PlayerSession>>(serverMethods.joinRoom, roomCode, name);
};

export const rejoinRoom = (session: PlayerSession) => connection.invoke<HubResponse<PlayerSession>>(serverMethods.rejoinRoom, session);

// Mobilen går tillbaka till /join oavsett utfall, så ett fel här har ingen att visas för.
export const leaveRoom = (session: PlayerSession) => connection.invoke(serverMethods.leaveRoom, session).catch(() => undefined);

export const removePlayer = (host: HostSession, playerId: string) =>
  connection.invoke(serverMethods.removePlayer, host, playerId).catch(() => undefined);

// null behåller inställningen som den är. Den nya bilden kommer med GameStateUpdated.
export const updateSettings = (host: HostSession, rounds: number | null, clipSeconds: number | null) =>
  connection.invoke<HubResponse<GameState>>(serverMethods.updateSettings, host, rounds, clipSeconds);

export const startGame = (host: HostSession) => connection.invoke<HubResponse<GameState>>(serverMethods.startGame, host);

// Bara en spelare i ett rum får söka. Sökrutan som använder den kommer i #19.
export const search = (query: string) => connection.invoke<HubResponse<Track[]>>(serverMethods.search, query);
