import { useState } from 'react';
import { UNREACHABLE } from '../../services/hub/connection.ts';
import type { HubError, HubResponse } from '../../services/hub/hubContract.ts';

// Svaret från run: antingen ett värde eller ett fel, så att if (response.error) säger vilket.
export type HubOutcome<T> = { value: T; error: null } | { value: null; error: HubError };

// Bara klienten använder koden, när anropet inte fick något svar alls. Därför står den inte i hubbkontraktet.
const unreachable: HubError = { code: 'hub.unreachable', message: UNREACHABLE };

// Ett hubbanrop som en knapp startar: väntan och felet som knappen och felraden visar. run kastar aldrig.
export const useHubCall = () => {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const run = async <T>(call: () => Promise<HubResponse<T>>): Promise<HubOutcome<T>> => {
    setBusy(true);
    setError(null);
    const response = await call().catch(() => null);
    setBusy(false);

    const outcome: HubOutcome<T> =
      response?.value != null ? { value: response.value, error: null } : { value: null, error: response?.error ?? unreachable };
    if (outcome.error) setError(outcome.error.message);
    return outcome;
  };

  return { busy, error, run };
};
