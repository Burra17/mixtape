const REPEATS = 6;

// Texten står två gånger efter varandra, så att bandet kan rulla ett halvt varv och börja om utan skarv.
export const Marquee = ({ text }: { text: string }) => {
  const half = `${text} • `.repeat(REPEATS);

  return (
    <div aria-hidden='true' className='overflow-hidden bg-ink whitespace-nowrap text-signal'>
      <div className='inline-block py-2 text-sm font-extrabold tracking-label uppercase lg:motion-safe:animate-marquee'>
        {half}
        {half}
      </div>
    </div>
  );
};
