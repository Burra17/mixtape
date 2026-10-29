import { useId } from 'react';

// Spåren är runda och syns inte rotera, så det är omslaget på etiketten som visar att skivan snurrar.
const GROOVES = [90, 80, 70, 60, 50];

// Skivan som sticker ut ur omslaget på storskärmen, med omslaget som rund etikett som på en riktig LP.
// Den stannar där den är när ljudet inte spelas, och fortsätter därifrån.
export const Vinyl = ({ coverUrl, spinning, className = '' }: { coverUrl: string; spinning: boolean; className?: string }) => {
  // Ett eget id per skiva, eftersom clipPath-id:n gäller för hela sidan.
  const labelId = useId();

  return (
    <svg viewBox='0 0 200 200' aria-hidden='true' className={`block h-auto ${className}`}>
      <defs>
        <clipPath id={labelId}>
          <circle cx={100} cy={100} r={34} />
        </clipPath>
      </defs>
      {/* Inline, eftersom animationsklassen sätter hela animation-förkortningen i en senare mediafråga och skulle skriva över en klass. */}
      <g
        className='origin-center [transform-box:fill-box] lg:motion-safe:animate-vinyl'
        style={{ animationPlayState: spinning ? 'running' : 'paused' }}
      >
        <circle cx={100} cy={100} r={98} className='fill-vinyl' />
        {GROOVES.map((r) => (
          <circle key={r} cx={100} cy={100} r={r} className='fill-none stroke-vinyl-groove' strokeWidth={1.5} />
        ))}
        {/* Den röda etiketten syns tills omslaget har laddats. */}
        <circle cx={100} cy={100} r={34} className='fill-vinyl-label' />
        <image href={coverUrl} x={66} y={66} width={68} height={68} preserveAspectRatio='xMidYMid slice' clipPath={`url(#${labelId})`} />
        <circle cx={100} cy={100} r={34} className='fill-none stroke-ink' strokeWidth={2} />
        <circle cx={100} cy={100} r={4} className='fill-paper' />
      </g>
    </svg>
  );
};
