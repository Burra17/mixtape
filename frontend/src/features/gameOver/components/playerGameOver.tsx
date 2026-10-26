import { Label } from '../../../shared/components/label.tsx';
import { PausedNotice } from '../../../shared/components/pausedNotice.tsx';
import { isSharedPlace, placeText } from '../../../shared/components/placeText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// Mobilen efter sista rundan: den egna slutplaceringen och poängen. Lobbyn visas av sig själv när värden trycker "Spela igen".
export const PlayerGameOver = () => {
  const session = usePlayerStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);

  const mine = game?.standings?.find((standing) => standing.playerId === session?.playerId);
  if (!game?.standings || !mine) return null;

  const result = mine.place !== 1 ? placeText(mine, game.standings) : isSharedPlace(mine, game.standings) ? 'Delad seger!' : 'Du vann!';

  return (
    <main className='flex min-h-dvh flex-col gap-6 px-6 py-12'>
      <Label>Slutställning</Label>
      <h1 className='text-4xl text-display leading-[1.25] [overflow-wrap:anywhere]'>{result}</h1>
      <p className='text-2xl font-extrabold tabular-nums'>{mine.score} poäng</p>
      <p role='status' className='text-lg font-bold'>
        Väntar på storskärmen.
      </p>
      {game.isPaused && <PausedNotice />}
    </main>
  );
};
