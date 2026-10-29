export type ButtonVariant = 'primary' | 'secondary';

const variantClasses: Record<ButtonVariant, string> = {
  primary: 'bg-ink text-signal',
  secondary: 'bg-card text-ink',
};

// Ramen, skuggan, fokus, trycket och det avstängda läget, som alla knappar delar.
const pressableClasses =
  'border-2 border-ink shadow-hard transition-transform focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink active:translate-x-0.5 active:translate-y-0.5 active:shadow-hard-pressed disabled:cursor-not-allowed disabled:opacity-50';

// Delas av Button och LinkButton, så att en knapp och en länk som ser ut som en knapp alltid är likadana.
export const buttonClasses = (variant: ButtonVariant) =>
  `inline-flex min-h-12 items-center justify-center rounded-control px-6 py-3 text-lg font-extrabold uppercase tracking-wide ${pressableClasses} lg:rounded-none ${variantClasses[variant]}`;

// Storskärmens kvadratiska ikonknapp, med skarpa hörn.
export const iconButtonClasses = `grid size-12 place-items-center bg-card ${pressableClasses}`;
