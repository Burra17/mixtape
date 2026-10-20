import { useState, type FormEvent } from 'react';
import { Button } from '../../../shared/components/button.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { useIsConnected } from '../../../store/connectionStore.ts';
import { useJoinRoom } from '../hooks/useJoinRoom.ts';

const fieldClasses =
  'min-h-12 w-full rounded-control border-2 border-ink bg-card px-4 text-lg font-semibold focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink lg:rounded-none';

const CODE_LENGTH = 4;

type JoinFormProps = {
  // Koden från QR-kodens länk. Utan den skriver spelaren in koden själv.
  roomFromLink: string | null;
  notice: string | null;
};

export const JoinForm = ({ roomFromLink, notice }: JoinFormProps) => {
  const { join, error, busy } = useJoinRoom();
  const isConnected = useIsConnected();
  const [roomCode, setRoomCode] = useState(roomFromLink ?? '');
  const [name, setName] = useState('');

  const submit = (event: FormEvent) => {
    event.preventDefault();
    void join(roomCode, name);
  };

  return (
    <main className='flex min-h-dvh flex-col justify-center gap-8 px-6 py-12'>
      <Label>Gå med</Label>
      <h1 className='text-5xl text-display'>{roomFromLink ? `Rum ${roomFromLink}` : 'Gå med'}</h1>

      {notice && (
        <p role='status' className='self-start bg-signal px-3 py-2 text-lg font-bold'>
          {notice}
        </p>
      )}

      <form onSubmit={submit} className='flex flex-col gap-6'>
        {!roomFromLink && (
          <label className='flex flex-col gap-2'>
            <span className='font-bold'>Rumskod</span>
            <input
              value={roomCode}
              onChange={(event) => setRoomCode(event.target.value.toUpperCase())}
              maxLength={CODE_LENGTH}
              autoCapitalize='characters'
              autoComplete='off'
              spellCheck={false}
              className={`${fieldClasses} tracking-[0.3em] uppercase`}
            />
          </label>
        )}

        <label className='flex flex-col gap-2'>
          <span className='font-bold'>Ditt namn</span>
          {/* Servern tillåter 16 tecken som de syns. maxLength räknar UTF-16-enheter, där en emoji kan vara flera, så gränsen här är bara ett tak. */}
          <input value={name} onChange={(event) => setName(event.target.value)} maxLength={32} autoComplete='nickname' className={fieldClasses} />
        </label>

        {error && (
          <p role='alert' className='text-lg font-bold'>
            {error}
          </p>
        )}

        <div>
          <Button type='submit' variant='primary' disabled={busy || !isConnected || roomCode.trim().length !== CODE_LENGTH || name.trim() === ''}>
            Gå med
          </Button>
        </div>
      </form>
    </main>
  );
};
