import { useEffect, useState } from 'react';

export type AppRoute = 'dashboard' | 'upload';

const dashboardHash = '#/dashboard';
const uploadHash = '#/upload';
const isBrowser = typeof window !== 'undefined';

function parseRoute(hash: string): AppRoute {
  return hash.toLowerCase() === uploadHash ? 'upload' : 'dashboard';
}

function toHash(route: AppRoute): string {
  return route === 'upload' ? uploadHash : dashboardHash;
}

export function useHashRoute() {
  const [route, setRoute] = useState<AppRoute>(() => parseRoute(isBrowser ? window.location.hash : ''));

  useEffect(() => {
    if (!isBrowser) {
      return;
    }

    if (!window.location.hash) {
      window.location.hash = dashboardHash;
    }

    const onHashChange = () => {
      setRoute(parseRoute(window.location.hash));
    };

    window.addEventListener('hashchange', onHashChange);
    return () => {
      window.removeEventListener('hashchange', onHashChange);
    };
  }, []);

  const navigate = (nextRoute: AppRoute) => {
    if (!isBrowser) {
      setRoute(nextRoute);
      return;
    }

    const nextHash = toHash(nextRoute);
    if (window.location.hash !== nextHash) {
      window.location.hash = nextHash;
    } else {
      setRoute(nextRoute);
    }
  };

  return { route, navigate };
}
