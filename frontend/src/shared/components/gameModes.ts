import type { GameMode } from '../../services/hub/hubContract.ts';

// Lägenas namn och beskrivning, som lobbyn på storskärmen och mobilen visar. Valen och gränserna kommer från servern.
export const GAME_MODES: Record<GameMode, { name: string; description: string }> = {
  auxCord: { name: 'Aux Cord Battles', description: 'Välj låten som passar texten. Alla röstar.' },
  dropTheNeedle: { name: 'Drop the Needle', description: 'Musikquiz – gissa låten först.' },
};
