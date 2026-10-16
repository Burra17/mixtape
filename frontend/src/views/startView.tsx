import { LinkButton } from '../shared/components/linkButton.tsx';

// Landningssidan för den som skriver in adressen för hand. TV:n och mobilerna går annars direkt till sina vyer.
export const StartView = () => (
  <main className='flex min-h-dvh flex-col items-center justify-center gap-10 p-6 text-center'>
    <div>
      <h1 className='text-6xl font-bold tracking-tight'>Mixtape</h1>
      <p className='mt-3 text-lg text-muted'>Musikspel för en kväll med kompisar</p>
    </div>
    <nav className='flex w-full max-w-xs flex-col gap-4'>
      <LinkButton to='/host' variant='primary'>
        Starta på TV:n
      </LinkButton>
      <LinkButton to='/join'>Gå med i ett spel</LinkButton>
    </nav>
  </main>
);
