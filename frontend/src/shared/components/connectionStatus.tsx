import { ping } from '../../services/hub/connection.ts';
import { useConnectionStore, type ConnectionStatus as Status } from '../../store/connectionStore.ts';
import { Button } from './button.tsx';
import { Card } from './card.tsx';
import { Label } from './label.tsx';

const statusText: Record<Status, string> = {
  connecting: 'Ansluter…',
  connected: 'Ansluten',
  reconnecting: 'Återansluter…',
  disconnected: 'Inte ansluten',
};

// Visar anslutningen till servern och låter en ping gå fram och tillbaka, så att hela kedjan går att prova.
export const ConnectionStatus = () => {
  const status = useConnectionStore((state) => state.status);
  const lastPongAt = useConnectionStore((state) => state.lastPongAt);

  return (
    <Card>
      <Label>Anslutning</Label>
      <p className='text-2xl font-extrabold' role='status'>
        {statusText[status]}
      </p>
      <p className='text-muted'>{lastPongAt ? `Senaste svar ${lastPongAt.toLocaleTimeString('sv-SE')}` : 'Inget svar än'}</p>
      <div>
        <Button onClick={() => void ping()} disabled={status !== 'connected'}>
          Skicka ping
        </Button>
      </div>
    </Card>
  );
};
