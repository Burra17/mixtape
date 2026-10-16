import { LinkButton } from '../shared/components/linkButton.tsx';

export const NotFoundView = () => (
  <main className='flex min-h-dvh flex-col items-center justify-center gap-6 p-6 text-center'>
    <h1 className='text-4xl font-bold'>Sidan finns inte</h1>
    <LinkButton to='/'>Till startsidan</LinkButton>
  </main>
);
