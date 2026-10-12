import { useCallback, useEffect, useState } from 'react';
import { Button } from '../../components/Button';
import { DataState } from '../../components/DataState';
import { PageHeader } from '../../components/PageHeader';
import { SectionCard } from '../../components/SectionCard';
import { StatusBadge } from '../../components/StatusBadge';
import { fetchHealth } from '../../gateway/healthGateway';
import type { AppRoute } from '../../hooks/useHashRoute';
import { formatCount, formatTimestamp } from '../../utils/formatting';

type DashboardState =
  | { kind: 'loading' }
  | { kind: 'ready'; healthStatus: string; checkedAt: string }
  | { kind: 'error'; message: string };

export function DashboardPage({ onNavigate }: { onNavigate: (route: AppRoute) => void }) {
  const [state, setState] = useState<DashboardState>({ kind: 'loading' });

  const loadDashboard = useCallback(async () => {
    setState({ kind: 'loading' });

    try {
      const result = await fetchHealth();
      setState({
        kind: 'ready',
        healthStatus: result.data.status,
        checkedAt: new Date().toISOString(),
      });
    } catch {
      setState({
        kind: 'error',
        message: 'Unable to load dashboard data from the API. Check the backend and try again.',
      });
    }
  }, []);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

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
        <SectionCard title="Operational overview" subtitle="Live health and known data availability">
          {state.kind === 'loading' && (
            <DataState
              kind="loading"
              title="Loading dashboard"
              description="Fetching API health and available operational signals."
            />
          )}

          {state.kind === 'error' && (
            <DataState
              kind="error"
              title="Dashboard unavailable"
              description={state.message}
              action={<Button onClick={() => void loadDashboard()}>Retry</Button>}
            />
          )}

          {state.kind === 'ready' && (
            <dl className="metric-list" aria-label="Operational metrics">
              <div>
                <dt>API health</dt>
                <dd>
                  <StatusBadge status={state.healthStatus || 'Unknown'} />
                </dd>
              </div>
              <div>
                <dt>Last checked</dt>
                <dd>{formatTimestamp(state.checkedAt)}</dd>
              </div>
              <div>
                <dt>Recent batches available</dt>
                <dd>Unavailable (history endpoint not implemented)</dd>
              </div>
              <div>
                <dt>Recent batch count</dt>
                <dd>{formatCount(null)}</dd>
              </div>
            </dl>
          )}
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
