import type { HostSession } from '../hub/hubContract.ts';

const KEY = 'mixtape.hostSession';

// localStorage kan vara avstängt eller fullt, och då fortsätter appen utan sparad session.
export const loadHostSession = (): HostSession | null => {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(KEY) ?? 'null');
    return isHostSession(parsed) ? parsed : null;
  } catch {
    return null;
  }
};

export const saveHostSession = (session: HostSession) => {
  try {
    localStorage.setItem(KEY, JSON.stringify(session));
  } catch {
    // Utan lagring går rummet ändå att använda, bara inte att återuppta efter en omladdning.
  }
};

export const clearHostSession = () => {
  try {
    localStorage.removeItem(KEY);
  } catch {
    // Inget att rensa om lagringen inte går att nå.
  }
};

const isHostSession = (value: unknown): value is HostSession =>
  typeof value === 'object' &&
  value !== null &&
  typeof (value as HostSession).roomCode === 'string' &&
  typeof (value as HostSession).hostToken === 'string';
