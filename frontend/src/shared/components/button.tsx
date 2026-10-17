import type { ComponentProps } from 'react';
import { buttonClasses, type ButtonVariant } from './buttonStyles.ts';

// className tas inte emot, eftersom egna klasser skulle krocka med knappens utseende.
type ButtonProps = Omit<ComponentProps<'button'>, 'className'> & {
  variant?: ButtonVariant;
};

export const Button = ({ variant = 'secondary', type = 'button', ...props }: ButtonProps) => (
  <button type={type} className={buttonClasses(variant)} {...props} />
);
