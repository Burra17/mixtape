import { Cassette } from '../shared/components/cassette.tsx';
import { LinkButton } from '../shared/components/linkButton.tsx';
import { Marquee } from '../shared/components/marquee.tsx';

// Landningssidan för den som skriver in adressen för hand. Laptopen och mobilerna går annars direkt till sina vyer.
export const StartView = () => (
  <div className='flex min-h-dvh flex-col'>
    <Marquee text='Mixtape • Musikspel för en kväll med kompisar' />
    <main className='flex flex-1 flex-col justify-center gap-8 px-6 py-12 lg:grid lg:grid-cols-2 lg:items-center lg:gap-[6vw] lg:px-[6vw]'>
      <Cassette className='w-40 lg:order-last lg:w-full lg:max-w-[40rem]' />
      <div className='flex flex-col gap-8'>
        {/* Avsteg från fasta storlekar: rubriken skalar med bredden, och från lg delar den raden med kassetten och krymper för att få plats. */}
        <h1 className='text-[clamp(3.5rem,11vw,10rem)] text-display lg:text-[clamp(3.5rem,6vw,8rem)]'>Mixtape</h1>
        <p className='max-w-[40ch] text-lg font-medium lg:text-2xl'>Välj den bästa låten till ögonblicket. Rösta fram kvällens DJ.</p>
        <nav className='flex flex-col gap-4 sm:flex-row'>
          <LinkButton to='/host' variant='primary'>
            Starta ett spel
          </LinkButton>
          <LinkButton to='/join'>Gå med i ett spel</LinkButton>
        </nav>
      </div>
    </main>
  </div>
);
