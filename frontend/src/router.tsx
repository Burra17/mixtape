import { createBrowserRouter } from 'react-router';
import { HostView } from './views/hostView.tsx';
import { JoinView } from './views/joinView.tsx';
import { NotFoundView } from './views/notFoundView.tsx';
import { PlayerView } from './views/playerView.tsx';
import { StartView } from './views/startView.tsx';

export const router = createBrowserRouter([
  { path: '/', element: <StartView /> },
  { path: '/host', element: <HostView /> },
  { path: '/join', element: <JoinView /> },
  { path: '/play', element: <PlayerView /> },
  { path: '*', element: <NotFoundView /> },
]);
