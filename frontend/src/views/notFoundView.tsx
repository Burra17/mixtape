import { Link } from 'react-router';

export const NotFoundView = () => (
  <main className='flex min-h-dvh flex-col items-center justify-center gap-6 p-6 text-center'>
    <h1 className='text-4xl font-bold'>Sidan finns inte</h1>
    <Link to='/' className='rounded-2xl bg-surface px-6 py-4 text-lg font-semibold'>
      Till startsidan
    </Link>
  </main>
);
