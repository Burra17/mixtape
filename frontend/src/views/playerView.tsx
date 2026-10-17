import { useEffect } from 'react';
import { startConnection } from '../services/hub/connection.ts';
import { ConnectionStatus } from '../shared/components/connectionStatus.tsx';

// Mobilens vy under spelet. Väljer fas utifrån serverns state när spelet finns.
export const PlayerView = () => {
  useEffect(() => {
    void startConnection();
  }, []);

  return (
    <main className='flex min-h-dvh flex-col justify-center gap-8 px-6 py-12'>
      <h1 className='text-5xl text-display'>Spelare</h1>
      <ConnectionStatus />
    </main>
  );
};
