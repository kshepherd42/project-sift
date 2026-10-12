import { AppShell } from './components/AppShell';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { BatchDetailsPlaceholderPage } from './features/batches/BatchDetailsPlaceholderPage';
import { UploadPage } from './features/upload/UploadPage';
import { useHashRoute } from './hooks/useHashRoute';

function App() {
  const { route, batchId, navigate, navigateToBatchDetails } = useHashRoute();

  let content = <DashboardPage onNavigate={navigate} />;

  if (route === 'upload') {
    content = <UploadPage onBatchCreated={navigateToBatchDetails} />;
  }

  if (route === 'batch-details' && batchId) {
    content = <BatchDetailsPlaceholderPage batchId={batchId} onNavigateToUpload={() => navigate('upload')} />;
  }

  return (
    <AppShell currentRoute={route} onNavigate={navigate}>
      {content}
    </AppShell>
  );
}

export default App;
