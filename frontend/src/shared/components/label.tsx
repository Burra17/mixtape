import type { ReactNode } from 'react';

// En etikett i hakparentes, som [RUMSKOD]. Hakparenteserna är en del av stilen, inte av texten.
export const Label = ({ children }: { children: ReactNode }) => (
  <span className='text-sm font-bold tracking-[0.12em] text-muted uppercase'>[{children}]</span>
);
