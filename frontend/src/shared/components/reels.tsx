const Reel = () => (
  <span className='block size-8 rounded-full border-[3px] border-ink bg-[radial-gradient(circle,var(--color-paper)_0_22%,transparent_23%),repeating-conic-gradient(var(--color-ink)_0_16deg,transparent_16deg_60deg)] lg:size-10 lg:motion-safe:animate-reel' />
);

// Kassettens två spolar. De snurrar bara på stor skärm, eftersom mobilen ska ligga still.
export const Reels = () => (
  <span aria-hidden='true' className='inline-flex gap-2'>
    <Reel />
    <Reel />
  </span>
);
