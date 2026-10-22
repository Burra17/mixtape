import { useState, type FormEvent } from 'react';
import { Button } from '../../../shared/components/button.tsx';
import { useIsConnected } from '../../../store/connectionStore.ts';
import type { useTrackPicker } from '../hooks/useTrackPicker.ts';
import { TrackRow } from './trackRow.tsx';

const ErrorLine = ({ error }: { error: string }) => (
  <p role='alert' className='text-lg font-bold'>
    {error}
  </p>
);

type TrackSearchProps = {
  picker: ReturnType<typeof useTrackPicker>;
};

// Sökrutan, träffarna och "Skicka in". Ett tryck markerar en låt och knappen bekräftar, eftersom bidraget är låst efteråt.
export const TrackSearch = ({ picker }: TrackSearchProps) => {
  const { results, selected, select, error, busy, find, submit } = picker;
  const isConnected = useIsConnected();
  const [query, setQuery] = useState('');
  const hasResults = results !== null && results.length > 0;

  const onSearch = (event: FormEvent) => {
    event.preventDefault();
    void find(query);
  };

  return (
    <>
      <form onSubmit={onSearch} role='search' className='flex gap-3'>
        <input
          type='search'
          enterKeyHint='search'
          aria-label='Sök låt eller artist'
          placeholder='Låt eller artist'
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          maxLength={100}
          autoComplete='off'
          className='min-h-12 w-full min-w-0 rounded-control border-2 border-ink bg-card px-4 text-lg font-semibold focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink lg:rounded-none'
        />
        <Button type='submit' disabled={busy || !isConnected || query.trim() === ''}>
          Sök
        </Button>
      </form>

      {/* Med träffar står felet vid "Skicka in", där spelaren är när "Någon hann före dig!" kommer. */}
      {error && !hasResults && <ErrorLine error={error} />}

      {results?.length === 0 && <p className='font-medium text-muted'>Inga träffar. Prova att söka på något annat.</p>}

      {hasResults && (
        <>
          <ul className='flex flex-col gap-2'>
            {results.map((track) => {
              const isSelected = selected?.id === track.id;
              return (
                <li key={track.id}>
                  <button
                    type='button'
                    aria-pressed={isSelected}
                    onClick={() => select(isSelected ? null : track)}
                    className={`flex min-h-12 w-full rounded-control border-2 p-2 focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink lg:rounded-none ${isSelected ? 'border-ink bg-card shadow-hard' : 'border-transparent'}`}
                  >
                    <TrackRow track={track} />
                  </button>
                </li>
              );
            })}
          </ul>

          {/* Knappen ligger kvar längst ned medan spelaren bläddrar bland träffarna. */}
          <div className='sticky bottom-0 -mx-6 flex flex-col gap-3 border-t-2 border-ink bg-paper px-6 py-4'>
            {error && <ErrorLine error={error} />}
            <Button variant='primary' onClick={() => void submit()} disabled={busy || !isConnected || !selected}>
              Skicka in
            </Button>
          </div>
        </>
      )}
    </>
  );
};
