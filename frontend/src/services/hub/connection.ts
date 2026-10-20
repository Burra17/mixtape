import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { useConnectionStore } from '../../store/connectionStore.ts';
import { clientEvents, serverMethods, type HostSession, type HubResponse } from './hubContract.ts';

// En enda anslutning för hela appen. Adressen är relativ, så Vite skickar den vidare till backenden.
const connection = new HubConnectionBuilder().withUrl('/gamehub').withAutomaticReconnect().configureLogging(LogLevel.Warning).build();

const { setStatus, receivePong } = useConnectionStore.getState();

connection.onreconnecting(() => setStatus('reconnecting'));
connection.onreconnected(() => setStatus('connected'));
connection.onclose(() => setStatus('disconnected'));
connection.on(clientEvents.pong, receivePong);

let starting: Promise<boolean> | null = null;

// Startar anslutningen om den inte redan är igång och svarar om den är uppe. Den som anropar under en pågående start får samma löfte.
export const startConnection = () => {
  if (connection.state === HubConnectionState.Disconnected) {
    setStatus('connecting');
    starting = connection.start().then(
      () => {
        setStatus('connected');
        return true;
      },
      () => {
        setStatus('disconnected');
        return false;
      },
    );
  }

  return starting ?? Promise.resolve(connection.state === HubConnectionState.Connected);
};

// Om anslutningen hinner försvinna innan svaret kommer visar statusen redan det, så felet behöver inte hanteras här.
export const ping = () => connection.invoke(serverMethods.ping).catch(() => undefined);

// previous skickas med vid "Nytt spel", så att servern tar bort det gamla rummet.
export const createRoom = (previous: HostSession | null) => connection.invoke<HubResponse<HostSession>>(serverMethods.createRoom, previous);

export const resumeRoom = (session: HostSession) => connection.invoke<HubResponse<HostSession>>(serverMethods.resumeRoom, session);

export const getJoinAddresses = () => connection.invoke<string[]>(serverMethods.getJoinAddresses);
