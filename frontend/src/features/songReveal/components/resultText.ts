import type { SongResultState } from '../../../services/hub/hubContract.ts';

// Vad spelaren fick för låten: "Rätt låt", "Rätt artist", "Fel" eller "Gissade inte".
export const resultText = (result: SongResultState) => {
  if (result.score === 'song') return 'Rätt låt';
  if (result.score === 'artist') return 'Rätt artist';
  return result.guess ? 'Fel' : 'Gissade inte';
};
