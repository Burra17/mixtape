import { useGameStore } from '../../store/gameStore.ts';

// Mobilens rad när spelet står still, i lobbyn och i alla faser. Texten säger varför, så att ingen oroar sig i onödan när någon bara gått på toa.
export const PausedNotice = () => {
  const reason = useGameStore((state) => state.game?.pauseReason);

  return (
    <p role='status' className='self-start rounded-control border-2 border-ink px-3 py-2 font-bold'>
      {reason === 'pausedByHost' ? 'Spelet är pausat.' : 'Storskärmen är borta. Spelet väntar.'}
    </p>
  );
};
