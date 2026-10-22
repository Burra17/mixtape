import { useState } from 'react';
import { search, submitTrack } from '../../../services/hub/connection.ts';
import type { Track } from '../../../services/hub/hubContract.ts';

const UNREACHABLE = 'Kunde inte nå servern. Försök igen.';

// Felen där låten inte går att välja, så att markeringen tas bort och spelaren väljer en annan.
const PICK_ANOTHER = ['entry.track_taken', 'music.track_unavailable'];

// Sökningen och inskicket på mobilen. Träffarna, markeringen och den inskickade låten är bara mobilens eget UI-state. Att spelaren är klar kommer från servern.
export const useTrackPicker = () => {
  const [results, setResults] = useState<Track[] | null>(null);
  const [selected, setSelected] = useState<Track | null>(null);
  const [submitted, setSubmitted] = useState<Track | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const find = async (query: string) => {
    setBusy(true);
    setError(null);
    const response = await search(query).catch(() => null);
    setBusy(false);

    if (!response?.value) {
      setError(response?.error?.message ?? UNREACHABLE);
      return;
    }

    setResults(response.value);
    setSelected(null);
  };

  const submit = async () => {
    if (!selected) return;
    setBusy(true);
    setError(null);
    const response = await submitTrack(selected.id).catch(() => null);
    setBusy(false);

    if (!response?.value) {
      setError(response?.error?.message ?? UNREACHABLE);
      if (response?.error && PICK_ANOTHER.includes(response.error.code)) setSelected(null);
      return;
    }

    setSubmitted(selected);
  };

  return { results, selected, select: setSelected, submitted, error, busy, find, submit };
};
