import { Label } from '../../../shared/components/label.tsx';

// Visas medan mobilen försöker få tillbaka sin plats, så att formuläret inte blinkar förbi.
export const Rejoining = () => (
  <main className='flex min-h-dvh flex-col justify-center gap-6 px-6 py-12'>
    <Label>Mixtape</Label>
    <p className='text-2xl font-bold'>Återansluter till rummet…</p>
  </main>
);
