// Etikettens ränder uppifrån och ned, i fallande tjocklek som på 80-talets kassetter.
const STRIPES = [
  { y: 18, height: 10, fillClass: 'fill-stripe-red' },
  { y: 28, height: 8, fillClass: 'fill-stripe-orange' },
  { y: 36, height: 6, fillClass: 'fill-stripe-blue' },
  { y: 42, height: 4, fillClass: 'fill-stripe-magenta' },
];

const SCREWS = [
  [18, 18],
  [302, 18],
  [18, 182],
  [302, 182],
];

// transform-box gör att spolen snurrar kring sin egen mitt och inte kring SVG:ns hörn.
const Reel = ({ cx }: { cx: number }) => (
  <g className='origin-center [transform-box:fill-box] lg:motion-safe:animate-reel'>
    <circle cx={cx} cy={102} r={17} className='fill-paper stroke-ink' strokeWidth={2.5} />
    <path d={`M${cx} 87v8M${cx} 109v8M${cx - 15} 102h8M${cx + 7} 102h8`} className='stroke-ink' strokeWidth={3} />
    <circle cx={cx} cy={102} r={5} className='fill-ink' />
  </g>
);

// Kassetten på startsidan och storskärmen. "A" i hörnet är spelläget Aux Cord Battles, sida A.
export const Cassette = ({ className = '' }: { className?: string }) => (
  <svg viewBox='0 0 320 200' aria-hidden='true' className={`block h-auto ${className}`}>
    <rect x={4} y={4} width={312} height={192} rx={10} className='fill-shell stroke-ink' strokeWidth={3} />
    {SCREWS.map(([cx, cy]) => (
      <circle key={`${cx}-${cy}`} cx={cx} cy={cy} r={4} className='fill-paper stroke-ink' strokeWidth={2} />
    ))}
    <rect x={30} y={18} width={260} height={118} className='fill-card' />
    {STRIPES.map(({ y, height, fillClass }) => (
      <rect key={y} x={30} y={y} width={260} height={height} className={fillClass} />
    ))}
    {/* Etikettens ram ritas efter ränderna, så att den ligger ovanpå dem. */}
    <rect x={30} y={18} width={260} height={118} className='fill-none stroke-ink' strokeWidth={2.5} />
    <text x={44} y={72} transform='rotate(-3 44 72)' className='fill-ink font-marker text-[22px]'>
      MIXTAPE
    </text>
    <text x={276} y={68} textAnchor='end' className='fill-ink text-[11px] font-black'>
      A
    </text>
    <rect x={78} y={80} width={164} height={44} rx={22} className='fill-shell-window stroke-ink' strokeWidth={2.5} />
    <rect x={118} y={90} width={84} height={24} className='fill-tape stroke-ink' strokeWidth={1.5} />
    <Reel cx={100} />
    <Reel cx={220} />
    <path d='M70 196 86 150h148l16 46' className='fill-shell stroke-ink' strokeWidth={2.5} />
    <circle cx={112} cy={174} r={5} className='fill-ink' />
    <circle cx={208} cy={174} r={5} className='fill-ink' />
    <rect x={148} y={168} width={24} height={10} className='fill-ink' />
  </svg>
);
