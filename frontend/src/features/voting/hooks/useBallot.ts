import { useEffect, useState } from 'react';
import { getBallot, UNREACHABLE, vote } from '../../../services/hub/connection.ts';
import type { Ballot } from '../../../services/hub/hubContract.ts';
import { useHubCall } from '../../../shared/hooks/useHubCall.ts';
import { usePlayerStore } from '../../../store/playerStore.ts';

// Mobilens röstsedel. Det egna bidraget och den egna rösten kommer från servern, bara till den här spelaren. Markeringen är mobilens eget UI-state.
export const useBallot = () => {
  // Räknas upp när spelaren kopplats till en ny anslutning. Först då känner servern igen anslutningen, så röstsedeln hämtas efter varje återanslutning.
  const sessionEpoch = usePlayerStore((state) => state.sessionEpoch);
  const [ballot, setBallot] = useState<Ballot | null>(null);
  // Utan röstsedeln vet mobilen inte vilket bidrag som är dess eget, så spelaren får veta det. Nästa återanslutning hämtar den igen.
  const [ballotError, setBallotError] = useState<string | null>(null);
  const [selected, setSelected] = useState<number | null>(null);
  const { busy, error, run } = useHubCall();

  useEffect(() => {
    let cancelled = false;
    void getBallot()
      .catch(() => null)
      .then((response) => {
        if (cancelled) return;
        if (response?.value) setBallot(response.value);
        setBallotError(response?.value ? null : (response?.error?.message ?? UNREACHABLE));
      });
    return () => {
      cancelled = true;
    };
  }, [sessionEpoch]);

  const castVote = async () => {
    if (selected === null) return;
    const response = await run(() => vote(selected));
    if (response.error) return;

    setBallot((previous) => ({ ownEntry: previous?.ownEntry ?? null, votedEntry: selected }));
  };

  return {
    ownEntry: ballot?.ownEntry ?? null,
    votedEntry: ballot?.votedEntry ?? null,
    ballotError,
    selected,
    select: setSelected,
    error,
    busy,
    castVote,
  };
};
