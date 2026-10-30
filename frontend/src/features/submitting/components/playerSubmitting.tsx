import { Countdown } from '../../../shared/components/countdown.tsx';
import { findPlayer } from '../../../shared/components/findPlayer.ts';
import { Label } from '../../../shared/components/label.tsx';
import { PausedNotice } from '../../../shared/components/pausedNotice.tsx';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';
import { submitTrack } from '../../../services/hub/connection.ts';
import { TrackRow } from '../../../shared/components/trackRow.tsx';
import { TrackSearch } from '../../../shared/components/trackSearch.tsx';
import { useTrackPicker } from '../../../shared/hooks/useTrackPicker.ts';

// Mobilen under inskicket: prompten, nedräkningen och sökningen. Efter inskicket väntar spelaren på de andra.
export const PlayerSubmitting = () => {
  const session = usePlayerStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();
  const picker = useTrackPicker(submitTrack);

  if (!game?.prompt) return null;

  // Servern säger om spelaren är klar, så att "Inskickad" står kvar efter en omladdning. Låten visas bara fram till dess.
  const hasSubmitted = findPlayer(game.players, session?.playerId)?.hasSubmitted ?? false;

  return (
    <main className='flex min-h-dvh flex-col gap-6 px-6 pt-12'>
      <Label>{round}</Label>
      <h1 className='text-3xl text-display leading-[1.08] [overflow-wrap:anywhere]'>{game.prompt}</h1>
      <Countdown screen='player' />
      {game.isPaused && <PausedNotice />}

      {hasSubmitted ? (
        <section className='flex flex-col gap-4 pb-12'>
          {picker.submitted && (
            <div className='rounded-control border-2 border-ink bg-card p-3 shadow-hard lg:rounded-none'>
              <TrackRow track={picker.submitted} />
            </div>
          )}
          <p role='status' className='text-lg font-bold'>
            Inskickad, väntar på de andra.
          </p>
        </section>
      ) : (
        <TrackSearch picker={picker} submitLabel='Skicka in' />
      )}
    </main>
  );
};
