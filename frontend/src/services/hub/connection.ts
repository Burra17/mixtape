import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { useConnectionStore } from '../../store/connectionStore.ts';
import { clientEvents, serverMethods } from './hubContract.ts';

// En enda anslutning för hela appen. Adressen är relativ, så Vite skickar den vidare till backenden.
const connection = new HubConnectionBuilder().withUrl('/gamehub').withAutomaticReconnect().configureLogging(LogLevel.Warning).build();

const { setStatus, receivePong } = useConnectionStore.getState();

connection.onreconnecting(() => setStatus('reconnecting'));
connection.onreconnected(() => setStatus('connected'));
connection.onclose(() => setStatus('disconnected'));
connection.on(clientEvents.pong, receivePong);

// Startar anslutningen om den inte redan är igång. StrictMode kör effekter två gånger, så ett andra anrop gör ingenting.
export const startConnection = async () => {
  if (connection.state !== HubConnectionState.Disconnected) return;

  setStatus('connecting');
  try {
    await connection.start();
    setStatus('connected');
  } catch {
    setStatus('disconnected');
  }
};

export const ping = () => connection.invoke(serverMethods.ping);
