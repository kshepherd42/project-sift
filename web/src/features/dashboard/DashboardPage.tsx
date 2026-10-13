import { useMemo, useState } from 'react';

import { Button } from '../../components/Button';
import { DataState } from '../../components/DataState';
import { PageHeader } from '../../components/PageHeader';
import { SectionCard } from '../../components/SectionCard';
import { formatTimestamp } from '../../utils/formatting';
import { loadRecentBatchActivity } from './recentBatchActivity';
import type { NavigableRoute } from '../routes/useHashRoute';

type DashboardPageProps = {
  onNavigate: (route: NavigableRoute) => void;
  onNavigateToBatchDetails: (batchId: string) => void;
};

export function DashboardPage({ onNavigate, onNavigateToBatchDetails }: DashboardPageProps) {
  const [batchLookupId, setBatchLookupId] = useState('');
  const recentActivity = useMemo(() => loadRecentBatchActivity(), []);

  const normalizedLookupId = batchLookupId.trim();

  return (
    <div className="stack-lg">
      <PageHeader
        title="Dashboard"
        subtitle="Start uploads, jump directly to a known batch ID, and review current platform limitations."
        actions={
          <Button onClick={() => onNavigate('upload')} aria-label="Go to upload page">
            Upload files
          </Button>
        }
      />

      <div className="dashboard-grid">
        <SectionCard title="Quick actions" subtitle="Most common workflow tasks.">
          <div className="stack-sm">
            <div className="button-row">
              <Button onClick={() => onNavigate('upload')}>Start a new upload</Button>
            </div>

            <label className="field stack-xs">
              <span>Open batch by ID</span>
              <input
                type="search"
                value={batchLookupId}
                onChange={(event) => setBatchLookupId(event.target.value)}
                placeholder="Paste batch ID"
                aria-label="Batch ID lookup"
              />
            </label>

            <div className="button-row">
              <Button
                variant="secondary"
                onClick={() => onNavigateToBatchDetails(normalizedLookupId)}
                disabled={normalizedLookupId.length === 0}
              >
                Open batch details
              </Button>
            </div>
          </div>
        </SectionCard>

        <SectionCard title="Recent activity" subtitle="Recently viewed batches in this browser.">
          {recentActivity.length === 0 ? (
            <DataState
              kind="empty"
              title="No recent activity yet"
              description="Open a batch details page and it will appear here for quick access."
            />
          ) : (
            <ul className="plain-list recent-batch-list" aria-label="Recent batches">
              {recentActivity.map((entry) => (
                <li key={entry.batchId}>
                  <button
                    type="button"
                    className="recent-batch-link"
                    onClick={() => onNavigateToBatchDetails(entry.batchId)}
                    aria-label={`Open batch ${entry.batchId}`}
                  >
                    <strong>{entry.batchId}</strong>
                    <span className="batch-muted">Viewed {formatTimestamp(entry.lastViewedUtc)}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </SectionCard>

        <SectionCard title="Terminology" subtitle="Short definitions used in batch details.">
          <DataState
            kind="loading"
            title="Payload = uploaded file"
            description="Document = a JSON object/array extracted from a payload. A payload can produce zero, one, or many documents."
          />
        </SectionCard>
      </div>
    </div>
  );
}
