import { PageHeader } from '../../components/PageHeader';
import { SectionCard } from '../../components/SectionCard';
import { formatFileSize, formatTimestamp } from '../../utils/formatting';

export function UploadPlaceholderPage() {
  return (
    <div className="stack-lg">
      <PageHeader
        title="Upload"
        subtitle="Stage 3 will implement full upload workflow. This placeholder keeps navigation explicit and non-misleading."
      />

      <SectionCard title="Coming next" subtitle="Planned for Stage 3">
        <ul className="plain-list">
          <li>Multi-file upload request composer</li>
          <li>Submission progress and error handling</li>
          <li>Batch identifier handoff to details view</li>
        </ul>
      </SectionCard>

      <SectionCard title="Shared formatting preview" subtitle="Current formatting conventions reused across pages.">
        <dl className="metric-list">
          <div>
            <dt>Current time</dt>
            <dd>{formatTimestamp(new Date())}</dd>
          </div>
          <div>
            <dt>Default file-size example</dt>
            <dd>{formatFileSize(2_097_152)}</dd>
          </div>
        </dl>
      </SectionCard>
    </div>
  );
}
