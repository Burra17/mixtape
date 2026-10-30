import type { PlayerSession, SettingsState } from '../../../services/hub/hubContract.ts';
import { Button } from '../../../shared/components/button.tsx';
import { GAME_MODES } from '../../../shared/components/gameModes.ts';
import { Label } from '../../../shared/components/label.tsx';
import { PausedNotice } from '../../../shared/components/pausedNotice.tsx';
import { playerText } from '../../../shared/components/playerColors.ts';
import { useIsConnected } from '../../../store/connectionStore.ts';
import { usePlayerLobby } from '../hooks/usePlayerLobby.ts';

const settingsLine = (settings: SettingsState) =>
  settings.mode === 'auxCord'
    ? `${settings.rounds} rundor · ${settings.clipSeconds} s klipp · ${settings.submittingSeconds} s betänketid`
    : `${settings.songs} låtar · ${settings.clipSeconds} s klipp`;

// Mobilen i lobbyn: ditt namn stort i din färg, hur många som är med, läget med inställningarna och "Lämna".
export const PlayerLobby = ({ session }: { session: PlayerSession }) => {
  const { game, me, leave } = usePlayerLobby(session);
  const isConnected = useIsConnected();

  return (
    <main className='flex min-h-dvh flex-col justify-between gap-12 px-6 py-12'>
      <div className='flex flex-col gap-6'>
        <Label>Rum {session.roomCode}</Label>
        {me && game ? (
          <>
            <h1 className={`text-5xl text-display [overflow-wrap:anywhere] ${playerText(me.colorIndex)}`}>{me.name}</h1>
            <p className='text-2xl font-extrabold'>
              {game.players.length} av {game.maxPlayers} spelare
            </p>
            <div className='flex flex-col gap-1'>
              <p className='text-2xl font-black uppercase'>{GAME_MODES[game.settings.mode].name}</p>
              <p className='text-lg font-bold'>{settingsLine(game.settings)}</p>
            </div>
            <p className='text-lg font-medium text-muted'>Väntar på att spelet startar</p>
            {game.isPaused && <PausedNotice />}
          </>
        ) : (
          <p className='text-2xl font-bold'>Hämtar lobbyn…</p>
        )}
      </div>
      <div>
        <Button onClick={() => void leave()} disabled={!isConnected}>
          Lämna
        </Button>
      </div>
    </main>
  );
};
