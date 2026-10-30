import { useId } from 'react';

const GROOVES = [90, 80, 70, 60, 50];
const LABEL_R = 34;

// Skivan på storskärmen, med omslaget som rund etikett. Spåren är runda, så det är etiketten som visar att den snurrar.
// Utan omslag står ett frågetecken på etiketten, som när låten ska gissas.
export const Vinyl = ({ coverUrl, spinning, className = '' }: { coverUrl?: string; spinning: boolean; className?: string }) => {
  // clipPath-id:n gäller för hela sidan, så varje skiva får ett eget.
  const labelId = useId();

  return (
    <svg viewBox='0 0 200 200' aria-hidden='true' className={`block h-auto ${className}`}>
      <defs>
        <clipPath id={labelId}>
          <circle cx={100} cy={100} r={LABEL_R} />
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
        {/* Röd tills omslaget har laddats. */}
        <circle cx={100} cy={100} r={LABEL_R} className='fill-vinyl-label' />
        {coverUrl ? (
          <image
            href={coverUrl}
            x={100 - LABEL_R}
            y={100 - LABEL_R}
            width={LABEL_R * 2}
            height={LABEL_R * 2}
            preserveAspectRatio='xMidYMid slice'
            clipPath={`url(#${labelId})`}
          />
        ) : (
          <text x={100} y={100} textAnchor='middle' dominantBaseline='central' className='fill-paper text-[40px] font-black'>
            ?
          </text>
        )}
        <circle cx={100} cy={100} r={LABEL_R} className='fill-none stroke-ink' strokeWidth={2} />
        <circle cx={100} cy={100} r={4} className='fill-paper' />
      </g>
    </svg>
  );
};
