// En millisekund tystnad som WAV, så att upplåsningen inte hörs och inte behöver hämtas.
const SILENCE = 'data:audio/wav;base64,UklGRiwAAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQgAAACAgICAgICAgA==';

// Safari släpper bara fram ljud från ett element som fått play() i ett klick, så alla klipp i #20 spelas på det här elementet.
const audio = new Audio();

// Anropas direkt i klicket på "Starta spel" och "Fortsätt", före hubbanropet, så att klicket fortfarande räknas.
export const unlockAudio = () => {
  audio.src = SILENCE;
  void audio.play().catch(() => undefined);
};
