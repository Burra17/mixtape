import { useEffect } from 'react';
import { startConnection } from '../services/hub/connection.ts';
import { ConnectionStatus } from '../shared/components/connectionStatus.tsx';

// Storskärmens vy. Väljer fas utifrån serverns state när spelet finns.
export const HostView = () => {
  useEffect(() => {
    void startConnection();
  }, []);

  return (
    <main className='flex min-h-dvh flex-col justify-center gap-8 px-6 py-12 lg:px-[6vw]'>
      <h1 className='text-[clamp(2.75rem,7vw,6rem)] text-display'>Storskärmen</h1>
      <div className='max-w-md'>
        <ConnectionStatus />
      </div>
    </main>
  );
};
