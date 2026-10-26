import type { CSSProperties } from 'react';

const REPEATS = 6;

// Varvtiden följer textens längd, så att bandet rullar lika fort på alla sidor.
const SECONDS_PER_CHARACTER = 0.18;

// Texten står två gånger efter varandra, så att bandet kan rulla ett halvt varv och börja om utan skarv.
export const Marquee = ({ text }: { text: string }) => {
  const half = `${text} • `.repeat(REPEATS);

  return (
    <div aria-hidden='true' className='overflow-hidden bg-ink whitespace-nowrap text-signal'>
      <div
        className='inline-block py-2 text-sm font-extrabold tracking-label uppercase lg:motion-safe:animate-marquee lg:motion-safe:will-change-transform'
        // Avsteg från klasser: varvtiden beror på texten och kan inte vara en tema-variabel, så bara längden sätts här och temat läser den.
        style={{ '--marquee-duration': `${half.length * SECONDS_PER_CHARACTER}s` } as CSSProperties}
      >
        {half}
        {half}
      </div>
    </div>
  );
};
