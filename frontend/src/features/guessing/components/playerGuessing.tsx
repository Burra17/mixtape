import { guess } from '../../../services/hub/connection.ts';
import { Countdown } from '../../../shared/components/countdown.tsx';
import { findPlayer } from '../../../shared/components/findPlayer.ts';
import { Label } from '../../../shared/components/label.tsx';
import { PausedNotice } from '../../../shared/components/pausedNotice.tsx';
import { TrackRow } from '../../../shared/components/trackRow.tsx';
import { TrackSearch } from '../../../shared/components/trackSearch.tsx';
import { useClipEnded } from '../../../shared/hooks/useClipEnded.ts';
import { useTrackPicker } from '../../../shared/hooks/useTrackPicker.ts';
import { useRoundText } from '../../../shared/hooks/useRoundText.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// Mobilen medan låten spelas på storskärmen: nedräkningen och sökningen. Gissningen är låst, och om den var rätt syns först i avslöjandet.
export const PlayerGuessing = () => {
  const session = usePlayerStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const round = useRoundText();
  const picker = useTrackPicker(guess);
  const clipEnded = useClipEnded();

  if (!game?.song) return null;

  // Servern säger om spelaren har gissat, så att det står kvar efter en omladdning. Låten visas bara fram till dess.
  const hasGuessed = findPlayer(game.players, session?.playerId)?.hasGuessed ?? false;

  return (
    <main className='flex min-h-dvh flex-col gap-6 px-6 pt-12'>
      <Label>{round}</Label>
      <h1 className='text-3xl text-display leading-[1.08]'>Vilken låt är det?</h1>
      <Countdown screen='player' />
      {clipEnded && (
        <p role='status' className='text-lg font-bold'>
          Klippet är slut – sista chansen att gissa
        </p>
      )}
      {game.isPaused && <PausedNotice />}

      {hasGuessed ? (
        <section className='flex flex-col gap-4 pb-12'>
          {picker.submitted && (
            <>
              <Label>Låst</Label>
              <div className='rounded-control border-2 border-ink bg-card p-3 shadow-hard lg:rounded-none'>
                <TrackRow track={picker.submitted} />
              </div>
            </>
          )}
          <p role='status' className='text-lg font-bold'>
            Väntar på de andra…
          </p>
        </section>
      ) : (
        <TrackSearch picker={picker} submitLabel='Gissa' />
      )}
    </main>
  );
};
