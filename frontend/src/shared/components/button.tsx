import type { ComponentProps } from 'react';
import { buttonClasses, type ButtonVariant } from './buttonStyles.ts';

type ButtonProps = ComponentProps<'button'> & {
  variant?: ButtonVariant;
};

export const Button = ({ variant = 'secondary', type = 'button', className = '', ...props }: ButtonProps) => (
  <button type={type} className={`${buttonClasses(variant)} ${className}`} {...props} />
);
