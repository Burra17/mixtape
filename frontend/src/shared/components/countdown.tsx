import { useCountdown } from '../hooks/useCountdown.ts';

const SIZES = {
  host: 'text-[clamp(4rem,10vw,10rem)]',
  player: 'text-6xl',
};

// Fasens nedräkning i hela sekunder. Visas inte i faser utan deadline.
export const Countdown = ({ screen }: { screen: 'host' | 'player' }) => {
  const seconds = useCountdown();
  if (seconds === null) return null;

  return <p className={`${SIZES[screen]} font-black tabular-nums`}>{seconds}</p>;
};
