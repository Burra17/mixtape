// Mobilens felrad, till exempel när servern säger nej till ett val.
export const ErrorLine = ({ error }: { error: string }) => (
  <p role='alert' className='text-lg font-bold'>
    {error}
  </p>
);
