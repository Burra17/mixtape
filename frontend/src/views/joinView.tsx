import { useSearchParams } from 'react-router';
import { JoinForm } from '../features/lobby/components/joinForm.tsx';
import { Rejoining } from '../features/lobby/components/rejoining.tsx';
import { usePlayerRejoin } from '../features/lobby/hooks/usePlayerRejoin.ts';
import { usePlayerStore } from '../store/playerStore.ts';

// Mobilens väg in i ett rum. QR-koden på storskärmen pekar hit med rumskoden i ?room=.
export const JoinView = () => {
  const [params] = useSearchParams();
  const notice = usePlayerStore((state) => state.notice);
  const roomFromLink = params.get('room')?.trim().toUpperCase() || null;
  const rejoining = usePlayerRejoin(roomFromLink);

  if (rejoining) return <Rejoining />;

  // key gör att formuläret börjar om när länken byts, till exempel efter en borttagning.
  return <JoinForm key={roomFromLink} roomFromLink={roomFromLink} notice={notice} />;
};
