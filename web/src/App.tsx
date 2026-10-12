import { AppShell } from './components/AppShell';
import { AppRouteContent } from './features/routes/AppRouteContent';
import { useHashRoute } from './features/routes/useHashRoute';

function App() {
  const { route, batchId, navigate, navigateToBatchDetails } = useHashRoute();

  return (
    <AppShell currentRoute={route} onNavigate={navigate}>
      <AppRouteContent
        route={route}
        batchId={batchId}
        onNavigate={navigate}
        onNavigateToBatchDetails={navigateToBatchDetails}
      />
    </AppShell>
  );
}

export default App;
