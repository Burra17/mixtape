import { useCountdown } from '../../../shared/hooks/useCountdown.ts';

// De sista sekunderna av avslöjandet visar storskärmen ställningen. Servern har 20 s för hela fasen, se RevealingDuration i GameRoom.
const STANDINGS_SECONDS = 8;

// Bytet räknas från serverns klocka, så att alla skärmar byter samtidigt, även efter en paus.
export const useShowsStandings = () => {
  const seconds = useCountdown();
  return seconds !== null && seconds <= STANDINGS_SECONDS;
};
