import { Button } from '../../../shared/components/button.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { Reels } from '../../../shared/components/reels.tsx';
import { useIsConnected } from '../../../store/connectionStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';

type HostStartProps = {
  onCreate: () => void;
  onResume: () => void;
  onStartOver: () => void;
};

// Storskärmen innan ett rum visas: skapa ett nytt, eller välj om det sparade rummet ska fortsätta.
export const HostStart = ({ onCreate, onResume, onStartOver }: HostStartProps) => {
  const screen = useHostStore((state) => state.screen);
  const session = useHostStore((state) => state.session);
  const error = useHostStore((state) => state.error);
  const isConnected = useIsConnected();

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text='Mixtape • Storskärmen' />
      <main className='flex flex-1 flex-col justify-center gap-8 px-6 py-12 lg:px-[6vw]'>
        <div className='flex items-center gap-4'>
          <Reels />
          <Label>Storskärmen</Label>
        </div>

        {screen === 'checking' && <p className='text-2xl font-bold'>Letar efter ett sparat rum…</p>}

        {screen === 'start' && (
          <>
            <h1 className='text-[clamp(3rem,9vw,8rem)] text-display'>Nytt spel</h1>
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
              <Button variant='primary' onClick={onResume}>
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
      </main>
    </div>
  );
};
