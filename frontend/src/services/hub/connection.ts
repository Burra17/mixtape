import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { useConnectionStore } from '../../store/connectionStore.ts';
import { useLobbyStore } from '../../store/lobbyStore.ts';
import { usePlayerStore } from '../../store/playerStore.ts';
import { clearPlayerSession } from '../storage/playerSessionStorage.ts';
import { clientEvents, serverMethods, type HostSession, type HubResponse, type LobbyState, type PlayerSession } from './hubContract.ts';

const FIRST_RETRY_DELAYS = [0, 2000];
const RETRY_INTERVAL = 5000;

// Försöker igen för evigt. En mobil kan vara låst i flera minuter, och då ska den komma tillbaka av sig själv.
const nextRetryDelay = (previousRetryCount: number) => FIRST_RETRY_DELAYS[previousRetryCount] ?? RETRY_INTERVAL;

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
connection.on(clientEvents.lobbyUpdated, (lobby: LobbyState) => useLobbyStore.getState().receiveLobby(lobby));
// Den borttagna mobilen lämnar själv rummets grupp, eftersom servern inte vet vilken anslutning den har.
connection.on(clientEvents.removedFromRoom, () => {
  const { session, removedFromRoom } = usePlayerStore.getState();
  if (session) void leaveRoom(session);
  clearPlayerSession();
  removedFromRoom();
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
        retryTimer = setTimeout(() => void startConnection(), RETRY_INTERVAL);
        return false;
      },
    );
  }

  return starting ?? Promise.resolve(connection.state === HubConnectionState.Connected);
};

// "Försök igen" och en sida som blir synlig igen väntar inte på nästa schemalagda försök.
export const retryConnection = async () => {
  if (connection.state === HubConnectionState.Reconnecting) {
    await connection.stop();
  }

  return startConnection();
};

// En mobil som låses upp har ofta en död anslutning som SignalR inte märkt än.
document.addEventListener('visibilitychange', () => {
  if (wanted && document.visibilityState === 'visible' && connection.state !== HubConnectionState.Connected) {
    void retryConnection();
  }
});

// previous skickas med vid "Nytt spel", så att servern tar bort det gamla rummet.
export const createRoom = (previous: HostSession | null) => connection.invoke<HubResponse<HostSession>>(serverMethods.createRoom, previous);

export const resumeRoom = (session: HostSession) => connection.invoke<HubResponse<HostSession>>(serverMethods.resumeRoom, session);

export const getJoinAddresses = () => connection.invoke<string[]>(serverMethods.getJoinAddresses);

export const joinRoom = (roomCode: string, name: string) => connection.invoke<HubResponse<PlayerSession>>(serverMethods.joinRoom, roomCode, name);

export const rejoinRoom = (session: PlayerSession) => connection.invoke<HubResponse<PlayerSession>>(serverMethods.rejoinRoom, session);

// Mobilen går tillbaka till /join oavsett utfall, så ett fel här har ingen att visas för.
export const leaveRoom = (session: PlayerSession) => connection.invoke(serverMethods.leaveRoom, session).catch(() => undefined);

export const removePlayer = (host: HostSession, playerId: string) =>
  connection.invoke(serverMethods.removePlayer, host, playerId).catch(() => undefined);
