import { BatchDetailsPage } from '../batches/BatchDetailsPage';
import { DashboardPage } from '../dashboard/DashboardPage';
import { UploadPage } from '../upload/UploadPage';
import type { AppRoute, NavigableRoute } from './useHashRoute';

type AppRouteContentProps = {
  route: AppRoute;
  batchId: string | null;
  onNavigate: (route: NavigableRoute) => void;
  onNavigateToBatchDetails: (batchId: string) => void;
};

export function AppRouteContent({ route, batchId, onNavigate, onNavigateToBatchDetails }: AppRouteContentProps) {
  if (route === 'upload') {
    return <UploadPage onBatchCreated={onNavigateToBatchDetails} />;
  }

  if (route === 'batch-details' && batchId) {
    return <BatchDetailsPage batchId={batchId} onNavigateToUpload={() => onNavigate('upload')} />;
  }

  return <DashboardPage onNavigate={onNavigate} onNavigateToBatchDetails={onNavigateToBatchDetails} />;
}
