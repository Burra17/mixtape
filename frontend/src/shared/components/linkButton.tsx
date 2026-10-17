import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { buttonClasses, type ButtonVariant } from './buttonStyles.ts';

type LinkButtonProps = {
  to: string;
  variant?: ButtonVariant;
  children: ReactNode;
};

// En länk som ser ut som en knapp. Navigering är en länk, så att den fungerar som en länk i webbläsaren.
export const LinkButton = ({ to, variant = 'secondary', children }: LinkButtonProps) => (
  <Link to={to} className={buttonClasses(variant)}>
    {children}
  </Link>
);
