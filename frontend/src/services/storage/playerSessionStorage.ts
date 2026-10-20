import type { PlayerSession } from '../hub/hubContract.ts';

const KEY = 'mixtape.playerSession';

// localStorage kan vara avstängt eller fullt, och då fortsätter appen utan sparad session.
export const loadPlayerSession = (): PlayerSession | null => {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(KEY) ?? 'null');
    return isPlayerSession(parsed) ? parsed : null;
  } catch {
    return null;
  }
};

export const savePlayerSession = (session: PlayerSession) => {
  try {
    localStorage.setItem(KEY, JSON.stringify(session));
  } catch {
    // Utan lagring går det ändå att spela, bara inte att komma tillbaka efter en omladdning.
  }
};

export const clearPlayerSession = () => {
  try {
    localStorage.removeItem(KEY);
  } catch {
    // Inget att rensa om lagringen inte går att nå.
  }
};

const isPlayerSession = (value: unknown): value is PlayerSession =>
  typeof value === 'object' &&
  value !== null &&
  typeof (value as PlayerSession).roomCode === 'string' &&
  typeof (value as PlayerSession).playerId === 'string' &&
  typeof (value as PlayerSession).playerToken === 'string';
