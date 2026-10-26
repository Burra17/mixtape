import { useEffect, useState } from 'react';
import { getBallot, vote } from '../../../services/hub/connection.ts';
import type { Ballot } from '../../../services/hub/hubContract.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

const UNREACHABLE = 'Kunde inte nå servern. Försök igen.';

// Mobilens röstsedel. Det egna bidraget och den egna rösten kommer från servern, bara till den här spelaren. Markeringen är mobilens eget UI-state.
export const useBallot = () => {
  // Räknas upp när spelaren kopplats till en ny anslutning. Först då känner servern igen anslutningen, så röstsedeln hämtas efter varje återanslutning.
  const sessionEpoch = usePlayerStore((state) => state.sessionEpoch);
  const [ballot, setBallot] = useState<Ballot | null>(null);
  const [selected, setSelected] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void getBallot().then(
      (response) => {
        if (!cancelled && response.value) setBallot(response.value);
      },
      () => undefined,
    );
    return () => {
      cancelled = true;
    };
  }, [sessionEpoch]);

  const castVote = async () => {
    if (selected === null) return;
    setBusy(true);
    setError(null);
    const response = await vote(selected).catch(() => null);
    setBusy(false);

    if (!response?.value) {
      setError(response?.error?.message ?? UNREACHABLE);
      return;
    }

    setBallot((previous) => ({ ownEntry: previous?.ownEntry ?? null, votedEntry: selected }));
  };

  return { ownEntry: ballot?.ownEntry ?? null, votedEntry: ballot?.votedEntry ?? null, selected, select: setSelected, error, busy, castVote };
};
