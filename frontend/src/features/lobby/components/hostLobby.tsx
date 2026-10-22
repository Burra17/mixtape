import { QRCodeSVG } from 'qrcode.react';
import { Card } from '../../../shared/components/card.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { Marquee } from '../../../shared/components/marquee.tsx';
import { useHostStore } from '../../../store/hostStore.ts';
import { HostPlayerList } from './hostPlayerList.tsx';
import { HostSetup } from './hostSetup.tsx';

// Laptopen öppnas ofta på localhost, som mobilerna inte når. Därför byggs adressen av serverns LAN-adress och sidans egen port.
const joinUrl = (address: string, roomCode: string) => {
  const port = window.location.port ? `:${window.location.port}` : '';
  return `http://${address}${port}/join?room=${roomCode}`;
};

export const HostLobby = () => {
  const session = useHostStore((state) => state.session);
  const joinAddresses = useHostStore((state) => state.joinAddresses);
  const addressIndex = useHostStore((state) => state.addressIndex);
  const nextAddress = useHostStore((state) => state.nextAddress);

  if (!session) return null;

  const address = joinAddresses[addressIndex] ?? window.location.hostname;
  const url = joinUrl(address, session.roomCode);
  const hasAlternatives = joinAddresses.length > 1;

  return (
    <div className='flex min-h-dvh flex-col'>
      <Marquee text='Väntar på spelare • Skanna koden med mobilen' />
      <main className='grid flex-1 items-center gap-10 px-6 py-12 lg:grid-cols-2 lg:px-[6vw]'>
        <div className='flex flex-col gap-6'>
          <Label>Rumskod</Label>
          <p className='self-start bg-signal px-4 pt-3 pb-1 text-[clamp(4rem,9vw,9rem)] text-display'>{session.roomCode}</p>
          <p className='max-w-[30ch] text-[clamp(1.25rem,2vw,1.75rem)] font-medium'>Skanna QR-koden med mobilen för att gå med.</p>
          <HostPlayerList />
          <HostSetup />
        </div>

        <Card>
          <Label>Gå med</Label>
          <div className='self-start bg-card p-4 text-ink'>
            <QRCodeSVG value={url} size={240} fgColor='currentColor' bgColor='transparent' title={`QR-kod till ${url}`} />
          </div>
          {hasAlternatives ? (
            <button
              type='button'
              onClick={nextAddress}
              aria-label={`Byt adress, nu ${address}`}
              className='self-start text-left font-semibold text-muted underline-offset-4 hover:underline focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink'
            >
              {url.replace('http://', '')} · byt adress
            </button>
          ) : (
            <p className='font-semibold text-muted'>{url.replace('http://', '')}</p>
          )}
        </Card>
      </main>
    </div>
  );
};
