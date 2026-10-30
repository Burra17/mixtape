import { Vinyl } from './vinyl.tsx';

type SleeveProps = {
  coverUrl: string;
  spinning: boolean;
  // Sätter --cover, omslagets storlek. Skivan är lika stor.
  className: string;
};

// Omslaget med skivan som sticker ut till höger, som ur ett fodral. Skivan syns från lg och snurrar medan klippet spelas.
export const Sleeve = ({ coverUrl, spinning, className }: SleeveProps) => (
  <div className={`relative shrink-0 lg:pr-[calc(var(--cover)/2)] ${className}`}>
    <Vinyl coverUrl={coverUrl} spinning={spinning} className='absolute top-0 right-0 hidden w-(--cover) lg:block' />
    <img src={coverUrl} alt='' className='relative aspect-square w-(--cover) border-2 border-ink bg-card shadow-hard' />
  </div>
);
