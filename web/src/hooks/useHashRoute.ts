import { useEffect, useState } from 'react';

export type AppRoute = 'dashboard' | 'upload' | 'batch-details';

type RouteState = {
  route: AppRoute;
  batchId: string | null;
};

const dashboardHash = '#/dashboard';
const uploadHash = '#/upload';
const batchesHashPrefix = '#/batches/';
const isBrowser = typeof window !== 'undefined';

function parseRoute(hash: string): RouteState {
  const normalizedHash = hash.toLowerCase();
  if (normalizedHash === uploadHash) {
    return { route: 'upload', batchId: null };
  }

  if (normalizedHash.startsWith(batchesHashPrefix)) {
    const rawBatchId = hash.slice(batchesHashPrefix.length).trim();
    if (rawBatchId.length > 0) {
      return { route: 'batch-details', batchId: decodeURIComponent(rawBatchId) };
    }
  }

  return { route: 'dashboard', batchId: null };
}

function toHash(route: AppRoute): string {
  if (route === 'upload') {
    return uploadHash;
  }

  return dashboardHash;
}

function toBatchHash(batchId: string): string {
  return `${batchesHashPrefix}${encodeURIComponent(batchId)}`;
}

export function useHashRoute() {
  const [state, setState] = useState<RouteState>(() => parseRoute(isBrowser ? window.location.hash : ''));

  useEffect(() => {
    if (!isBrowser) {
      return;
    }

    if (!window.location.hash) {
      window.location.hash = dashboardHash;
    }

    const onHashChange = () => {
      setState(parseRoute(window.location.hash));
    };

    window.addEventListener('hashchange', onHashChange);
    return () => {
      window.removeEventListener('hashchange', onHashChange);
    };
  }, []);

  const navigate = (nextRoute: AppRoute) => {
    if (!isBrowser) {
      setState({ route: nextRoute, batchId: null });
      return;
    }

    const nextHash = toHash(nextRoute);
    if (window.location.hash !== nextHash) {
      window.location.hash = nextHash;
    } else {
      setState({ route: nextRoute, batchId: null });
    }
  };

  const navigateToBatchDetails = (batchId: string) => {
    const normalizedBatchId = batchId.trim();
    if (!normalizedBatchId) {
      return;
    }

    if (!isBrowser) {
      setState({ route: 'batch-details', batchId: normalizedBatchId });
      return;
    }

    const nextHash = toBatchHash(normalizedBatchId);
    if (window.location.hash !== nextHash) {
      window.location.hash = nextHash;
    } else {
      setState({ route: 'batch-details', batchId: normalizedBatchId });
    }
  };

  return { route: state.route, batchId: state.batchId, navigate, navigateToBatchDetails };
}
