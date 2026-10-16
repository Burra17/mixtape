import { useSearchParams } from 'react-router';

// Mobilens väg in i ett rum. QR-koden på TV:n pekar hit med rumskoden i ?room=.
export const JoinView = () => {
  const [searchParams] = useSearchParams();
  const roomCode = searchParams.get('room');

  return (
    <main className='flex min-h-dvh flex-col items-center justify-center gap-3 p-6 text-center'>
      <h1 className='text-4xl font-bold'>Gå med</h1>
      {roomCode && <p className='text-muted'>Rum {roomCode}</p>}
    </main>
  );
};
