import { useEffect, useState } from 'react';
import { retryConnection } from '../../services/hub/connection.ts';
import { useConnectionStore } from '../../store/connectionStore.ts';
import { Button } from './button.tsx';

// Efter så här lång tid utan anslutning byts "Återansluter…" mot "Tappade anslutningen". Försöken fortsätter ändå i bakgrunden.
const GIVE_UP_AFTER_MS = 30_000;

// Den gula raden överst på både storskärmen och mobilen när anslutningen är borta.
export const ConnectionBar = () => {
  const status = useConnectionStore((state) => state.status);
  const lostSince = useConnectionStore((state) => state.lostSince);
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    if (lostSince === null) return;
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, [lostSince]);

  if (status !== 'reconnecting' || lostSince === null) return null;

  const gaveUp = now - lostSince >= GIVE_UP_AFTER_MS;

  return (
    <div
      role='status'
      className='sticky top-0 z-10 flex min-h-14 items-center justify-between gap-4 border-b-2 border-ink bg-signal px-6 py-2 font-bold lg:px-[6vw] lg:text-[clamp(1.25rem,1.6vw,1.75rem)]'
    >
      <span>{gaveUp ? 'Tappade anslutningen' : 'Återansluter…'}</span>
      {gaveUp && <Button onClick={() => void retryConnection()}>Försök igen</Button>}
    </div>
  );
};
