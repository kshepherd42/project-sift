import { Button } from '../../components/Button';
import { DataState } from '../../components/DataState';
import { PageHeader } from '../../components/PageHeader';
import { SectionCard } from '../../components/SectionCard';
import type { NavigableRoute } from '../routes/useHashRoute';

export function DashboardPage({ onNavigate }: { onNavigate: (route: NavigableRoute) => void }) {
  return (
    <div className="stack-lg">
      <PageHeader
        title="Dashboard"
        subtitle="Operational overview based on currently available backend contracts."
        actions={
          <Button onClick={() => onNavigate('upload')} aria-label="Go to upload page">
            Upload files
          </Button>
        }
      />

      <div className="dashboard-grid">
        <SectionCard title="Operational overview" subtitle="Live operational metrics are not available in current backend contracts.">
          <DataState
            kind="unavailable"
            title="Operational overview unavailable"
            description="This stage does not expose live operational or aggregate metrics in the dashboard. Operational details will be added when the required contract is available."
            action={<Button onClick={() => onNavigate('upload')}>Start a new upload</Button>}
          />
        </SectionCard>

        <SectionCard title="Recent activity" subtitle="Newest-first history cannot be loaded without list/history contract.">
          <DataState
            kind="unavailable"
            title="Recent activity unavailable"
            description="The backend currently supports batch submission and by-id retrieval only. History, search, and pagination are not yet available."
            action={<Button onClick={() => onNavigate('upload')}>Start a new upload</Button>}
          />
        </SectionCard>
      </div>
    </div>
  );
}
