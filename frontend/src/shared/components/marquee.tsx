const REPEATS = 6;

// Ett rullande textband. Texten finns två gånger efter varandra, så att bandet kan rulla ett halvt varv och börja om utan skarv.
// Det rör sig bara på stor skärm, eftersom mobilen ska ligga still.
export const Marquee = ({ text }: { text: string }) => {
  const half = `${text} • `.repeat(REPEATS);

  return (
    <div aria-hidden='true' className='overflow-hidden bg-ink whitespace-nowrap text-signal'>
      <div className='inline-block py-2 text-sm font-extrabold tracking-[0.14em] uppercase lg:motion-safe:animate-marquee'>
        {half}
        {half}
      </div>
    </div>
  );
};
