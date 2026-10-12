import { Button } from '../../components/Button';
import { DataState } from '../../components/DataState';
import { PageHeader } from '../../components/PageHeader';
import { SectionCard } from '../../components/SectionCard';

type BatchDetailsPlaceholderPageProps = {
  batchId: string;
  onNavigateToUpload: () => void;
};

export function BatchDetailsPlaceholderPage({ batchId, onNavigateToUpload }: BatchDetailsPlaceholderPageProps) {
  return (
    <div className="stack-lg">
      <PageHeader
        title="Batch details"
        subtitle="Stage 4 will complete full batch inspection. This page confirms successful batch creation and routing."
      />

      <SectionCard title="Batch accepted" subtitle="Returned by POST /v1/batches.">
        <dl className="metric-list" aria-label="Batch details summary">
          <div>
            <dt>Batch ID</dt>
            <dd>{batchId}</dd>
          </div>
        </dl>
      </SectionCard>

      <SectionCard title="Details view pending" subtitle="Expanded payload/document rendering arrives in Stage 4.">
        <DataState
          kind="unavailable"
          title="Detailed batch view unavailable"
          description="Submission is complete, but full status/details rendering is deferred to Stage 4 implementation."
          action={<Button onClick={onNavigateToUpload}>Submit another batch</Button>}
        />
      </SectionCard>
    </div>
  );
}
