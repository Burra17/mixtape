// Spåren är runda och syns inte rotera, så det är strecket på etiketten som visar att skivan snurrar.
const GROOVES = [90, 80, 70, 60, 50];

// Skivan som sticker ut ur omslaget på storskärmen. Den stannar där den är när ljudet inte spelas, och fortsätter därifrån.
export const Vinyl = ({ spinning, className = '' }: { spinning: boolean; className?: string }) => (
  <svg viewBox='0 0 200 200' aria-hidden='true' className={`block h-auto ${className}`}>
    <g className={`origin-center [transform-box:fill-box] lg:motion-safe:animate-vinyl ${spinning ? '' : '[animation-play-state:paused]'}`}>
      <circle cx={100} cy={100} r={98} className='fill-vinyl' />
      {GROOVES.map((r) => (
        <circle key={r} cx={100} cy={100} r={r} className='fill-none stroke-vinyl-groove' strokeWidth={1.5} />
      ))}
      <circle cx={100} cy={100} r={34} className='fill-vinyl-label stroke-ink' strokeWidth={2} />
      <rect x={96} y={72} width={8} height={20} className='fill-signal' />
      <circle cx={100} cy={100} r={4} className='fill-paper' />
    </g>
  </svg>
);
