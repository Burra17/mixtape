import { Link } from 'react-router';

// Landningssidan för den som skriver in adressen för hand. TV:n och mobilerna går annars direkt till sina vyer.
export const StartView = () => (
  <main className='flex min-h-dvh flex-col items-center justify-center gap-10 p-6 text-center'>
    <div>
      <h1 className='text-6xl font-bold tracking-tight'>Mixtape</h1>
      <p className='mt-3 text-lg text-muted'>Musikspel för en kväll med kompisar</p>
    </div>
    <nav className='flex w-full max-w-xs flex-col gap-4'>
      <Link to='/host' className='rounded-2xl bg-accent px-6 py-4 text-lg font-semibold text-background'>
        Starta på TV:n
      </Link>
      <Link to='/join' className='rounded-2xl bg-surface px-6 py-4 text-lg font-semibold'>
        Gå med i ett spel
      </Link>
    </nav>
  </main>
);
