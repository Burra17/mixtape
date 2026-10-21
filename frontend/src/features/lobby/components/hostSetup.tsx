import { Button } from '../../../shared/components/button.tsx';
import { Card } from '../../../shared/components/card.tsx';
import { Label } from '../../../shared/components/label.tsx';
import { useIsConnected } from '../../../store/connectionStore.ts';
import { useGameStore } from '../../../store/gameStore.ts';
import { useGameSetup } from '../hooks/useGameSetup.ts';

type OptionRowProps = {
  label: string;
  options: number[];
  selected: number;
  format: (value: number) => string;
  disabled: boolean;
  onChoose: (value: number) => void;
};

// En rad knappar där en i taget är vald. Den valda är svart, som en nedtryckt knapp.
const OptionRow = ({ label, options, selected, format, disabled, onChoose }: OptionRowProps) => (
  <div role='group' aria-label={label} className='flex flex-col gap-2'>
    <Label>{label}</Label>
    <div className='flex flex-wrap gap-3'>
      {options.map((option) => (
        <button
          key={option}
          type='button'
          aria-pressed={option === selected}
          disabled={disabled}
          onClick={() => onChoose(option)}
          className={`min-h-12 min-w-20 border-2 border-ink px-5 text-[clamp(1.125rem,1.6vw,1.5rem)] font-extrabold tabular-nums focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink disabled:cursor-not-allowed disabled:opacity-50 ${option === selected ? 'bg-ink text-signal' : 'bg-card text-ink'}`}
        >
          {format(option)}
        </button>
      ))}
    </div>
  </div>
);

// Värdens inställningar och "Starta spel" på storskärmen. Valen och gränsen för antal spelare kommer från servern.
export const HostSetup = () => {
  const game = useGameStore((state) => state.game);
  const isConnected = useIsConnected();
  const { choose, start, error } = useGameSetup();

  if (!game) return null;

  return (
    <Card>
      <OptionRow
        label='Rundor'
        options={game.roundOptions}
        selected={game.settings.rounds}
        format={String}
        disabled={!isConnected}
        onChoose={(rounds) => choose(rounds, game.settings.clipSeconds)}
      />
      <OptionRow
        label='Klipplängd'
        options={game.clipSecondsOptions}
        selected={game.settings.clipSeconds}
        format={(seconds) => `${seconds} s`}
        disabled={!isConnected}
        onChoose={(clipSeconds) => choose(game.settings.rounds, clipSeconds)}
      />
      {!game.canStart && <p className='font-medium text-muted'>Minst {game.minPlayers} spelare behövs.</p>}
      {error && (
        <p role='alert' className='font-bold'>
          {error}
        </p>
      )}
      <div>
        <Button variant='primary' onClick={start} disabled={!game.canStart || !isConnected}>
          Starta spel
        </Button>
      </div>
    </Card>
  );
};
