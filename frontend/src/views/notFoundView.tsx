import { Label } from '../shared/components/label.tsx';
import { LinkButton } from '../shared/components/linkButton.tsx';

export const NotFoundView = () => (
  <main className='flex min-h-dvh flex-col justify-center gap-8 px-6 py-12 lg:px-[6vw]'>
    <Label>404</Label>
    <h1 className='text-[clamp(2.75rem,8vw,7rem)] text-display'>Sidan finns inte</h1>
    <div>
      <LinkButton to='/'>Till startsidan</LinkButton>
    </div>
  </main>
);
