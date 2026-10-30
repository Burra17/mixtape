import { useEffect, useState, useSyncExternalStore } from 'react';
import { finishClip, getIsPlaying, playClip, resumeClip, stopClip, subscribeToPlaying } from '../../services/audio/audioPlayer.ts';
import { getClip } from '../../services/hub/connection.ts';
import { useIsConnected } from '../../store/connectionStore.ts';
import { useGameStore, useRoomGame } from '../../store/gameStore.ts';
import { useHostStore } from '../../store/hostStore.ts';
import { useClipEnded } from './useClipEnded.ts';

// Hur långt in i klippet servern är, räknat från när bilden kom fram, så att ljudet slutar samtidigt som serverns klocka.
// I Drop the Needle är fasen klippet och sedan betänketiden.
const secondsPlayed = () => {
  const { game, receivedAt } = useGameStore.getState();
  if (!game || game.remainingMs === null) return 0;
  const left = game.remainingMs - (Date.now() - receivedAt);
  const phaseSeconds = game.settings.clipSeconds + (game.settings.mode === 'dropTheNeedle' ? game.settings.thinkingSeconds : 0);
  return Math.max(0, phaseSeconds - left / 1000);
};

// Storskärmen spelar klippet som servern säger. Ett nytt klipp, en hävd paus och en återanslutning hämtar länken och börjar där servern är.
// Servern äger klockan, så ett klipp som inte går att spela visas ändå, och TV:n följer klockan till nästa klipp.
// playing är sant bara när ljudet faktiskt hörs, inte medan länken hämtas, under paus, utan anslutning eller när klippet buffrar.
export const useClipPlayback = () => {
  const session = useHostStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const isConnected = useIsConnected();
  const [failedIndex, setFailedIndex] = useState<number | null>(null);
  const isPlaying = useSyncExternalStore(subscribeToPlaying, getIsPlaying);
  // Under betänketiden är det tyst. Klippet spelas inte igen.
  const clipEnded = useClipEnded();

  // Aux Cord Battles klipp står i nowPlaying och Drop the Needles låt i song.
  const index = game?.nowPlaying?.index ?? game?.song?.clip ?? null;
  const canPlay = session !== null && index !== null && !game?.isPaused && isConnected && !clipEnded;

  useEffect(() => {
    if (clipEnded) finishClip();
  }, [clipEnded]);

  useEffect(() => {
    if (!canPlay || !session) return;
    let cancelled = false;

    const start = async () => {
      // Avslöjandet spelar vidare på klippet från gissningen, som gissningens vy stoppade när den försvann. Servern lämnar bara ut klippet
      // under gissningen, så efter en omladdning eller "Fortsätt", som laddar upplåsningens tystnad, förblir avslöjandet tyst.
      if (useGameStore.getState().game?.phase === 'songReveal') {
        await resumeClip().catch(() => undefined);
        return;
      }
      const response = await getClip(session).catch(() => null);
      if (cancelled) return;
      // Ett svar för ett klipp som redan bytts ut kastas. Bilden för det nya klippet startar sin egen hämtning.
      if (response?.value && response.value.index !== index) return;

      try {
        if (!response?.value) throw new Error(response?.error?.message ?? 'Kunde inte nå servern.');
        await playClip(response.value.previewUrl, secondsPlayed());
        if (!cancelled) setFailedIndex(null);
      } catch (error) {
        if (cancelled) return;
        console.error(`Klipp ${index} gick inte att spela.`, error);
        setFailedIndex(index);
      }
    };

    void start();
    return () => {
      cancelled = true;
      stopClip();
    };
  }, [canPlay, session, index]);

  return {
    failed: failedIndex !== null && failedIndex === index,
    playing: canPlay && isPlaying,
  };
};
