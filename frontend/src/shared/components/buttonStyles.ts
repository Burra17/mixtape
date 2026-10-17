export type ButtonVariant = 'primary' | 'secondary';

const variantClasses: Record<ButtonVariant, string> = {
  primary: 'bg-ink text-signal',
  secondary: 'bg-card text-ink',
};

// Delas av Button och LinkButton, så att en knapp och en länk som ser ut som en knapp alltid är likadana.
// Hörnen är skarpa på stor skärm och lätt rundade på mobilen, där tummen ska träffa.
export const buttonClasses = (variant: ButtonVariant) =>
  `inline-flex min-h-12 items-center justify-center rounded-md border-2 border-ink px-6 py-3 text-lg font-extrabold uppercase tracking-wide shadow-[4px_4px_0_var(--color-ink)] transition-transform focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink active:translate-x-0.5 active:translate-y-0.5 active:shadow-[2px_2px_0_var(--color-ink)] disabled:cursor-not-allowed disabled:opacity-50 lg:rounded-none ${variantClasses[variant]}`;
