import type { GameStanding } from '../../../services/hub/hubContract.ts';
import { Countdown } from '../../../shared/components/countdown.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { PausedNotice } from '../../../shared/components/pausedNotice.tsx';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// "1:a" och "2:a", annars ":e", som "3:e" och "8:e".
const ordinal = (place: number) => `${place}:${place === 1 || place === 2 ? 'a' : 'e'}`;

// "2:a av 4", eller "Delad 1:a av 4" när någon annan har samma placering.
const placeText = (mine: GameStanding, standings: GameStanding[]) => {
  const shared = standings.filter((standing) => standing.place === mine.place).length > 1;
  return `${shared ? 'Delad ' : ''}${ordinal(mine.place)} av ${standings.length}`;
};

// Mobilen under avslöjandet: rundans poäng, placeringen och den totala poängen.
export const PlayerRevealing = () => {
  const session = usePlayerStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();

  const mine = game?.standings?.find((standing) => standing.playerId === session?.playerId);
  if (!game?.standings || !mine) return null;

  return (
    <main className='flex min-h-dvh flex-col gap-6 px-6 py-12'>
      <Label>{round}</Label>
      {/* Mindre än platshållarens rubrik, så att "Avslöjande" får plats på en smal mobil. */}
      <h1 className='text-3xl text-display [overflow-wrap:anywhere]'>Avslöjande</h1>
      <p className='text-5xl font-black tabular-nums'>+{mine.roundPoints} poäng</p>
      <p className='text-2xl font-extrabold'>{placeText(mine, game.standings)}</p>
      <p className='text-lg text-muted'>{mine.score} poäng totalt</p>
      <Countdown screen='player' />
      {game.isPaused && <PausedNotice />}
    </main>
  );
};
