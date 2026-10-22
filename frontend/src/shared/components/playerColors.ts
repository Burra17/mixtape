// Klassnamnen står utskrivna, eftersom Tailwind bara hittar klasser som finns som hel text i koden. colorIndex 1 är första platsen.
const backgrounds = ['bg-player-1', 'bg-player-2', 'bg-player-3', 'bg-player-4', 'bg-player-5', 'bg-player-6', 'bg-player-7', 'bg-player-8'];
const texts = [
  'text-player-1',
  'text-player-2',
  'text-player-3',
  'text-player-4',
  'text-player-5',
  'text-player-6',
  'text-player-7',
  'text-player-8',
];

export const playerBackground = (colorIndex: number) => backgrounds[colorIndex - 1] ?? 'bg-ink';

export const playerText = (colorIndex: number) => texts[colorIndex - 1] ?? 'text-ink';
