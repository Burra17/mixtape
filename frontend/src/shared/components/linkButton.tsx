import type { ReactNode } from 'react';
import { Link } from 'react-router';

type LinkButtonProps = {
  to: string;
  variant?: 'primary' | 'secondary';
  children: ReactNode;
};

const variantClasses = {
  primary: 'bg-accent text-background',
  secondary: 'bg-surface',
};

// En länk som ser ut som en stor knapp. Navigering är en länk, inte en knapp, så att den fungerar som en länk i webbläsaren.
export const LinkButton = ({ to, variant = 'secondary', children }: LinkButtonProps) => (
  <Link to={to} className={`rounded-2xl px-6 py-4 text-lg font-semibold ${variantClasses[variant]}`}>
    {children}
  </Link>
);
