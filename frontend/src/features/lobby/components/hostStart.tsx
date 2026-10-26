import { Button } from '../../../shared/components/button.tsx';
import { Cassette } from '../../../shared/components/cassette.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { useIsConnected } from '../../../store/connectionStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';

type HostStartProps = {
  onCreate: () => void;
  onResume: () => void;
  onStartOver: () => void;
};

// Storskärmen innan ett rum visas: skapa ett nytt, eller välj om det sparade rummet ska fortsätta. Samma upplägg med kassetten som startsidan.
export const HostStart = ({ onCreate, onResume, onStartOver }: HostStartProps) => {
  const screen = useHostStore((state) => state.screen);
  const session = useHostStore((state) => state.session);
  const error = useHostStore((state) => state.error);
  const roomClosed = useHostStore((state) => state.roomClosed);
  const isConnected = useIsConnected();

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text='Mixtape • Storskärmen' />
      <main className='flex flex-1 flex-col justify-center gap-8 px-6 py-12 lg:grid lg:grid-cols-2 lg:items-center lg:gap-[6vw] lg:px-[6vw]'>
        <Cassette className='w-40 lg:order-last lg:w-full lg:max-w-[40rem]' />
        <div className='flex flex-col gap-8'>
          {screen === 'checking' && <p className='text-2xl font-bold'>Letar efter ett sparat rum…</p>}

          {screen === 'start' && (
            <>
              <h1 className='text-[clamp(3rem,9vw,8rem)] text-display'>Nytt spel</h1>
              {roomClosed && (
                <p role='status' className='self-start border-2 border-ink px-4 py-2 text-[clamp(1.25rem,2vw,1.75rem)] font-bold'>
                  Det förra rummet har stängts.
                </p>
              )}
              <div>
                <Button variant='primary' onClick={onCreate} disabled={!isConnected}>
                  Skapa spel
                </Button>
              </div>
            </>
          )}

          {screen === 'resumable' && session && (
            <>
              <h1 className='text-[clamp(3rem,9vw,8rem)] text-display'>Rum {session.roomCode}</h1>
              <p className='max-w-[40ch] text-2xl font-medium'>Rummet finns kvar. Fortsätt där ni var, eller börja om med ett nytt rum.</p>
              <div className='flex flex-col gap-4 sm:flex-row'>
                <Button variant='primary' onClick={onResume} disabled={!isConnected}>
                  Fortsätt
                </Button>
                <Button onClick={onStartOver} disabled={!isConnected}>
                  Nytt spel
                </Button>
              </div>
            </>
          )}

          {error && (
            <p role='alert' className='text-xl font-bold'>
              {error}
            </p>
          )}
        </div>
      </main>
    </div>
  );
};
