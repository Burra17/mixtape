// En millisekund tystnad som WAV, så att upplåsningen inte hörs och inte behöver hämtas.
const SILENCE = 'data:audio/wav;base64,UklGRiwAAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQgAAACAgICAgICAgA==';

// Safari släpper bara fram ljud från ett element som fått play() i ett klick, så alla klipp spelas på det här elementet.
const audio = new Audio();

// Anropas direkt i klicket på "Starta spel" och "Fortsätt", före hubbanropet, så att klicket fortfarande räknas.
export const unlockAudio = () => {
  audio.src = SILENCE;
  void audio.play().catch(() => undefined);
};

// Startar from sekunder in i klippet, så att ett klipp som redan pågått slutar samtidigt som serverns klocka. Löftet avvisas om klippet inte går att spela.
// Sant när klippet har spelat sin tid, så att avslöjandet inte börjar om på resten av Deezers klipp efter betänketidens tystnad.
let finished = false;

export const playClip = (url: string, from: number) => {
  finished = false;
  audio.src = url;
  audio.currentTime = from;
  return audio.play();
};

export const stopClip = () => audio.pause();

export const finishClip = () => {
  finished = true;
  audio.pause();
};

// Spelar vidare där klippet stoppades, när samma klipp fortsätter i en ny vy. Gör inget om inget klipp är laddat, som efter upplåsningen i "Fortsätt", eller om klippet redan spelat klart.
export const resumeClip = () => (audio.src && audio.src !== SILENCE && !audio.ended && !finished ? audio.play() : Promise.resolve());

// Om ljud faktiskt hörs just nu. Elementets egna händelser, så att ett klipp som buffrar, får ett fel eller tar slut räknas som tyst.
let isPlaying = false;
const listeners = new Set<() => void>();
const setPlaying = (playing: boolean) => {
  if (playing === isPlaying) return;
  isPlaying = playing;
  listeners.forEach((listener) => listener());
};
audio.addEventListener('playing', () => setPlaying(true));
for (const event of ['pause', 'ended', 'waiting', 'error', 'emptied']) audio.addEventListener(event, () => setPlaying(false));

// För useSyncExternalStore.
export const subscribeToPlaying = (listener: () => void) => {
  listeners.add(listener);
  return () => listeners.delete(listener);
};
export const getIsPlaying = () => isPlaying;
