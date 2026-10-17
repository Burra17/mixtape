import type { ReactNode } from 'react';

// En panel för det som hör ihop, som rumskoden eller spelarlistan. Ramen och skuggan skiljer den från bakgrunden.
export const Card = ({ children }: { children: ReactNode }) => (
  <div className='flex flex-col gap-4 rounded-control border-2 border-ink bg-card p-6 shadow-hard lg:rounded-none'>{children}</div>
);
