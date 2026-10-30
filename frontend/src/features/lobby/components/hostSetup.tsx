import type { ModeOptions, SettingsState } from '../../../services/hub/hubContract.ts';
import { Button } from '../../../shared/components/button.tsx';
import { Card } from '../../../shared/components/card.tsx';
import { GAME_MODES } from '../gameModes.ts';
import { Label } from '../../../shared/components/label.tsx';
import { useIsConnected } from '../../../store/connectionStore.ts';
import { useRoomGame } from '../../../store/gameStore.ts';
import { useHostStore } from '../../../store/hostStore.ts';
import { useGameSetup } from '../hooks/useGameSetup.ts';

// Ett val, inte ett steg, så inte buttonStyles.ts: utan skugga, och det valda är svart med ljus text för att inte likna primärknappen.
const choiceStyle = (selected: boolean) =>
  `border-2 border-ink focus-visible:outline-3 focus-visible:outline-offset-4 focus-visible:outline-ink disabled:cursor-not-allowed disabled:opacity-50 ${selected ? 'bg-ink text-paper' : 'bg-card text-ink'}`;

type OptionRowProps = {
  label: string;
  options: number[];
  selected: number;
  format: (value: number) => string;
  disabled: boolean;
  onChoose: (value: number) => void;
};

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
          className={`min-h-12 min-w-20 px-5 text-[clamp(1.125rem,1.6vw,1.5rem)] font-extrabold tabular-nums ${choiceStyle(option === selected)}`}
        >
          {format(option)}
        </button>
      ))}
    </div>
  </div>
);

type ModeCardProps = {
  options: ModeOptions;
  selected: boolean;
  disabled: boolean;
  onChoose: () => void;
};

// Ett spelläge med namn, beskrivning och gränsen för antal spelare.
const ModeCard = ({ options, selected, disabled, onChoose }: ModeCardProps) => (
  <button
    type='button'
    aria-pressed={selected}
    disabled={disabled}
    onClick={onChoose}
    className={`flex flex-1 flex-col gap-2 p-5 text-left ${choiceStyle(selected)}`}
  >
    <span className='text-[clamp(1.25rem,2vw,1.875rem)] font-black uppercase'>{GAME_MODES[options.mode].name}</span>
    <span className='text-[clamp(1rem,1.3vw,1.25rem)] font-medium'>{GAME_MODES[options.mode].description}</span>
    <span className='text-[clamp(0.875rem,1.1vw,1rem)] font-bold'>Minst {options.minPlayers} spelare</span>
  </button>
);

const seconds = (value: number) => `${value} s`;

// Värdens val av läge, lägets inställningar och "Starta spel" på storskärmen. Lägena, valen och gränsen för antal spelare kommer från servern.
export const HostSetup = () => {
  const session = useHostStore((state) => state.session);
  const game = useRoomGame(session?.roomCode);
  const isConnected = useIsConnected();
  const { selectMode, setAuxCordSettings, setDropTheNeedleSettings, start, error } = useGameSetup();

  if (!game) return null;

  // Drop the Needle går att välja och ställa in, men spelet kommer i #70.
  const isReady = game.settings.mode === 'auxCord';

  return (
    <Card>
      <div role='group' aria-label='Spelläge' className='flex flex-col gap-2'>
        <Label>Spelläge</Label>
        <div className='flex flex-wrap gap-3'>
          {game.modes.map((options) => (
            <ModeCard
              key={options.mode}
              options={options}
              selected={options.mode === game.settings.mode}
              disabled={!isConnected}
              onChoose={() => selectMode(options.mode)}
            />
          ))}
        </div>
      </div>
      <ModeSettings
        settings={game.settings}
        modes={game.modes}
        disabled={!isConnected}
        onAuxCord={setAuxCordSettings}
        onDropTheNeedle={setDropTheNeedleSettings}
      />
      {!game.canStart && <p className='font-medium text-muted'>Minst {game.minPlayers} spelare behövs.</p>}
      {error && (
        <p role='alert' className='font-bold'>
          {error}
        </p>
      )}
      <div>
        <Button variant='primary' onClick={start} disabled={!game.canStart || !isConnected || !isReady}>
          {isReady ? 'Starta spel' : 'Kommer snart'}
        </Button>
      </div>
    </Card>
  );
};

type ModeSettingsProps = {
  settings: SettingsState;
  modes: ModeOptions[];
  disabled: boolean;
  onAuxCord: (rounds: number | null, clipSeconds: number | null, submittingSeconds: number | null) => void;
  onDropTheNeedle: (songs: number | null, clipSeconds: number | null) => void;
};

// Det valda lägets inställningar, med valen för samma läge.
const ModeSettings = ({ settings, modes, disabled, onAuxCord, onDropTheNeedle }: ModeSettingsProps) => {
  if (settings.mode === 'auxCord') {
    const options = modes.find((mode) => mode.mode === 'auxCord');
    if (!options) return null;
    return (
      <>
        <OptionRow
          label='Rundor'
          options={options.roundOptions}
          selected={settings.rounds}
          format={String}
          disabled={disabled}
          onChoose={(rounds) => onAuxCord(rounds, null, null)}
        />
        <OptionRow
          label='Klipplängd'
          options={options.clipSecondsOptions}
          selected={settings.clipSeconds}
          format={seconds}
          disabled={disabled}
          onChoose={(clipSeconds) => onAuxCord(null, clipSeconds, null)}
        />
        <OptionRow
          label='Betänketid'
          options={options.submittingSecondsOptions}
          selected={settings.submittingSeconds}
          format={seconds}
          disabled={disabled}
          onChoose={(submittingSeconds) => onAuxCord(null, null, submittingSeconds)}
        />
      </>
    );
  }

  const options = modes.find((mode) => mode.mode === 'dropTheNeedle');
  if (!options) return null;
  return (
    <>
      <OptionRow
        label='Låtar'
        options={options.songOptions}
        selected={settings.songs}
        format={String}
        disabled={disabled}
        onChoose={(songs) => onDropTheNeedle(songs, null)}
      />
      <OptionRow
        label='Klipplängd'
        options={options.clipSecondsOptions}
        selected={settings.clipSeconds}
        format={seconds}
        disabled={disabled}
        onChoose={(clipSeconds) => onDropTheNeedle(null, clipSeconds)}
      />
    </>
  );
};
