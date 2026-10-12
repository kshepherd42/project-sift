import { AppShell } from './components/AppShell';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { UploadPlaceholderPage } from './features/upload/UploadPlaceholderPage';
import { useHashRoute } from './hooks/useHashRoute';

function App() {
  const { route, navigate } = useHashRoute();

  return (
    <AppShell currentRoute={route} onNavigate={navigate}>
      {route === 'upload' ? <UploadPlaceholderPage /> : <DashboardPage onNavigate={navigate} />}
    </AppShell>
  );
}

export default App;
