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
export const playClip = (url: string, from: number) => {
  audio.src = url;
  audio.currentTime = from;
  return audio.play();
};

export const stopClip = () => audio.pause();
