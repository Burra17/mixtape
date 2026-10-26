const REPEATS = 6;

// Ett tecken är ungefär 10 px brett, så bandet rullar runt 55 px/s. Varvtiden följer textens längd, så att farten blir densamma på alla sidor.
const SECONDS_PER_CHARACTER = 0.18;

// Texten står två gånger efter varandra, så att bandet kan rulla ett halvt varv och börja om utan skarv.
export const Marquee = ({ text }: { text: string }) => {
  const half = `${text} • `.repeat(REPEATS);

  return (
    <div aria-hidden='true' className='overflow-hidden bg-ink whitespace-nowrap text-signal'>
      <div
        className='inline-block py-2 text-sm font-extrabold tracking-label uppercase lg:motion-safe:animate-marquee lg:motion-safe:will-change-transform'
        style={{ animationDuration: `${half.length * SECONDS_PER_CHARACTER}s` }}
      >
        {half}
        {half}
      </div>
    </div>
  );
};
