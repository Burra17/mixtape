import { useState } from 'react';
import { search, submitTrack } from '../../../services/hub/connection.ts';
import type { Track } from '../../../services/hub/hubContract.ts';
import { useHubCall } from '../../../shared/hooks/useHubCall.ts';

// Felen där låten inte går att välja, så att markeringen tas bort och spelaren väljer en annan.
const PICK_ANOTHER = ['entry.track_taken', 'music.track_unavailable'];

// Sökningen och inskicket på mobilen. Träffarna, markeringen och den inskickade låten är bara mobilens eget UI-state. Att spelaren är klar kommer från servern.
export const useTrackPicker = () => {
  const [results, setResults] = useState<Track[] | null>(null);
  const [selected, setSelected] = useState<Track | null>(null);
  const [submitted, setSubmitted] = useState<Track | null>(null);
  const { busy, error, run } = useHubCall();

  const find = async (query: string) => {
    const response = await run(() => search(query));
    if (response.error) return;

    setResults(response.value);
    setSelected(null);
  };

  const submit = async () => {
    if (!selected) return;
    const response = await run(() => submitTrack(selected.id));
    if (response.error) {
      if (PICK_ANOTHER.includes(response.error.code)) setSelected(null);
      return;
    }

    setSubmitted(selected);
  };

  return { results, selected, select: setSelected, submitted, error, busy, find, submit };
};
