import { useSearchParams } from 'react-router';
import { JoinForm } from '../features/lobby/components/joinForm.tsx';
import { usePlayerStore } from '../store/playerStore.ts';

// Mobilens väg in i ett rum. QR-koden på storskärmen pekar hit med rumskoden i ?room=.
export const JoinView = () => {
  const [params] = useSearchParams();
  const notice = usePlayerStore((state) => state.notice);
  const roomFromLink = params.get('room')?.trim().toUpperCase() || null;

  // key gör att formuläret börjar om när länken byts, till exempel efter en borttagning.
  return <JoinForm key={roomFromLink} roomFromLink={roomFromLink} notice={notice} />;
};
