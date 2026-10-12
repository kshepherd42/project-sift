import { useState } from 'react';

import { Button } from '../../components/Button';
import { DataState } from '../../components/DataState';
import { PageHeader } from '../../components/PageHeader';
import { SectionCard } from '../../components/SectionCard';
import type { NavigableRoute } from '../routes/useHashRoute';

type DashboardPageProps = {
  onNavigate: (route: NavigableRoute) => void;
  onNavigateToBatchDetails: (batchId: string) => void;
};

export function DashboardPage({ onNavigate, onNavigateToBatchDetails }: DashboardPageProps) {
  const [batchLookupId, setBatchLookupId] = useState('');

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

        <SectionCard title="Recent activity" subtitle="History/search across all batches requires a list endpoint that is not yet available.">
          <DataState
            kind="unavailable"
            title="Recent activity unavailable"
            description="The backend currently supports batch submission and by-id retrieval only. Keep your batch IDs to revisit details later."
          />
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
