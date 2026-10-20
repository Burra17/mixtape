import { RouterProvider } from 'react-router';
import { router } from './router.tsx';
import { ConnectionBar } from './shared/components/connectionBar.tsx';

// Raden för anslutningen ligger utanför routern, så att den syns på varje vy som har startat anslutningen.
export const App = () => (
  <>
    <ConnectionBar />
    <RouterProvider router={router} />
  </>
);
