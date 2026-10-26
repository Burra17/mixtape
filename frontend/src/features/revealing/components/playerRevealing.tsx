import { Countdown } from '../../../shared/components/countdown.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { PausedNotice } from '../../../shared/components/pausedNotice.tsx';
import { placeText } from '../../../shared/components/placeText.ts';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

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
      {/* Liten nog för att "Avslöjande" ska få plats på en smal mobil. */}
      <h1 className='text-3xl text-display [overflow-wrap:anywhere]'>Avslöjande</h1>
      <p className='text-5xl font-black tabular-nums'>+{mine.roundPoints} poäng</p>
      <p className='text-2xl font-extrabold'>{placeText(mine, game.standings)}</p>
      <p className='text-lg text-muted'>{mine.score} poäng totalt</p>
      <Countdown screen='player' />
      {game.isPaused && <PausedNotice />}
    </main>
  );
};
