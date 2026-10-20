import type { PlayerSession } from '../hub/hubContract.ts';

const KEY = 'mixtape.playerSession';

// Sparas redan nu, men läses först när mobilen kan återansluta (#11).
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
