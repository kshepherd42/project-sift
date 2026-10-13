import { useMemo, useState } from 'react';

import { Button } from '../../components/Button';
import { DataState } from '../../components/DataState';
import { PageHeader } from '../../components/PageHeader';
import { SectionCard } from '../../components/SectionCard';
import { StatusBadge } from '../../components/StatusBadge';
import { formatCount, formatDuration, formatTimestamp } from '../../utils/formatting';
import {
  deriveBatchDetailsSummary,
  getDocumentHeading,
  getDocumentNameLabel,
  getDocumentTypeLabel,
  getPayloadHeading,
  getPayloadNameLabel,
  type BatchDiagnostic,
  type BatchDocument,
  type BatchPayload,
} from './batchDetailsModel';
import {
  describeDiagnosticMatch,
  describeSearchMatch,
  searchBatchDetails,
  searchBatchPathOccurrences,
  type BatchPathMode,
} from './batchInsightsModel';
import {
  buildBatchExportFilename,
  copyTextToClipboard,
  createBatchCsvRows,
  createBatchJsonExport,
  serializeBatchCsv,
  serializeBatchJsonExport,
  triggerDownload,
} from './batchExport';
import { interpretDocumentStatus } from './batchDetailsStatus';
import { useBatchDetails } from './useBatchDetails';

type BatchDetailsPageProps = {
  batchId: string;
  onNavigateToUpload: () => void;
};

function DiagnosticsPanel({ diagnostics, scopeLabel }: { diagnostics: BatchDiagnostic[]; scopeLabel: string }) {
  if (diagnostics.length === 0) {
    return <p className="batch-muted">No diagnostics reported.</p>;
  }

  return (
    <details className="diagnostics-panel">
      <summary>{diagnostics.length} diagnostic{diagnostics.length === 1 ? '' : 's'} · View technical details</summary>
      <ul className="plain-list diagnostics-list" aria-label={`${scopeLabel} diagnostics`}>
        {diagnostics.map((diagnostic) => (
          <li key={diagnostic.key}>
            <strong>{diagnostic.code}</strong>
            <div>{diagnostic.message}</div>
            {diagnostic.position !== null && <div className="batch-muted">Position: {diagnostic.position}</div>}
          </li>
        ))}
      </ul>
    </details>
  );
}

function DocumentGroup({
  document,
  order,
  onCopyPropertyPath,
}: {
  document: BatchDocument;
  order: number;
  onCopyPropertyPath: (propertyPath: string) => void;
}) {
  const status = interpretDocumentStatus(document.status);

  return (
    <details className="result-group result-group-document">
      <summary>
        <div>
          <strong>{getDocumentHeading(document, order)}</strong>
          <div className="batch-muted">{getDocumentNameLabel(document)} · {getDocumentTypeLabel(document)}</div>
        </div>
        <StatusBadge status={status.label} tone={status.tone} />
      </summary>

      <div className="result-group-body stack-sm">
        <div className="batch-subsection">
          <h4>Property paths</h4>
          {document.propertyPaths.length === 0 ? (
            <p className="batch-muted">No property paths available.</p>
          ) : (
            <ul className="plain-list property-path-list">
              {document.propertyPaths.map((propertyPath, index) => (
                <li key={`${document.key}-path-${index}`}>
                  <span>{propertyPath}</span>
                  <Button
                    variant="secondary"
                    aria-label={`Copy property path ${propertyPath}`}
                    onClick={() => onCopyPropertyPath(propertyPath)}
                  >
                    Copy
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="batch-subsection">
          <h4>Diagnostics</h4>
          <DiagnosticsPanel diagnostics={document.diagnostics} scopeLabel={getDocumentHeading(document, order)} />
        </div>
      </div>
    </details>
  );
}

function PayloadGroup({
  payload,
  order,
  onCopyPropertyPath,
}: {
  payload: BatchPayload;
  order: number;
  onCopyPropertyPath: (propertyPath: string) => void;
}) {
  const summary = deriveBatchDetailsSummary({
    batchId: 'payload-only',
    status: 'Completed',
    payloads: [payload],
    createdUtc: null,
    completedUtc: null,
    responseWarnings: [],
  });

  return (
    <details className="result-group result-group-payload">
      <summary>
        <div>
          <strong>{getPayloadHeading(payload, order)}</strong>
          <div className="batch-muted">{getPayloadNameLabel(payload)}</div>
          <div className="batch-muted">
            {formatCount(summary.documentCount)} documents · {formatCount(payload.diagnostics.length)} diagnostics
          </div>
        </div>
      </summary>

      <div className="result-group-body stack-sm">
        <div className="batch-subsection">
          <h4>Payload diagnostics</h4>
          <DiagnosticsPanel diagnostics={payload.diagnostics} scopeLabel={getPayloadHeading(payload, order)} />
        </div>

        <div className="batch-subsection">
          <h4>Documents</h4>
          {payload.documents.length === 0 ? (
            <p className="batch-muted">No documents were returned for this payload.</p>
          ) : (
            <div className="stack-sm">
              {payload.documents.map((document, documentOrder) => (
                <DocumentGroup
                  key={document.key}
                  document={document}
                  order={documentOrder}
                  onCopyPropertyPath={onCopyPropertyPath}
                />
              ))}
            </div>
          )}
        </div>
      </div>
    </details>
  );
}

export function BatchDetailsPage({ batchId, onNavigateToUpload }: BatchDetailsPageProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [pathMode, setPathMode] = useState<BatchPathMode>('occurrences');
  const [feedbackMessage, setFeedbackMessage] = useState<string | null>(null);

  async function copyPropertyPath(propertyPath: string) {
    try {
      await copyTextToClipboard(propertyPath);
      setFeedbackMessage(`Copied ${propertyPath} to the clipboard.`);
    } catch {
      setFeedbackMessage('Clipboard access is unavailable in this browser context.');
    }
  }

  const {
    batch,
    summary,
    blockingError,
    refreshWarning,
    isInitialLoading,
    isRefreshing,
    isAutoRefreshPaused,
    lastUpdatedAt,
    currentElapsedMs,
    isLongRunning,
    refresh,
  } = useBatchDetails(batchId);

  const searchResults = useMemo(() => {
    if (!batch) {
      return null;
    }

    return searchBatchDetails(batch, searchQuery);
  }, [batch, searchQuery]);

  const matchingPathOccurrences = useMemo(() => {
    if (!batch) {
      return [];
    }

    return searchBatchPathOccurrences(batch, searchQuery, pathMode);
  }, [batch, pathMode, searchQuery]);

  const matchingDiagnosticEntries = searchResults?.diagnosticEntries ?? [];

  async function handleCopyMatchingPaths() {
    if (matchingPathOccurrences.length === 0) {
      setFeedbackMessage('No matching property paths were available to copy.');
      return;
    }

    try {
      await copyTextToClipboard(matchingPathOccurrences.map(describeSearchMatch).join('\n'));
      setFeedbackMessage(`Copied ${formatCount(matchingPathOccurrences.length)} path${matchingPathOccurrences.length === 1 ? '' : 's'} to the clipboard.`);
    } catch {
      setFeedbackMessage('Clipboard access is unavailable in this browser context.');
    }
  }

  async function handleCopyDiagnosticMatches() {
    if (matchingDiagnosticEntries.length === 0) {
      setFeedbackMessage('No matching diagnostics were available to copy.');
      return;
    }

    try {
      await copyTextToClipboard(matchingDiagnosticEntries.map(describeDiagnosticMatch).join('\n'));
      setFeedbackMessage(`Copied ${formatCount(matchingDiagnosticEntries.length)} diagnostic${matchingDiagnosticEntries.length === 1 ? '' : 's'} to the clipboard.`);
    } catch {
      setFeedbackMessage('Clipboard access is unavailable in this browser context.');
    }
  }

  function handleExportJson() {
    if (!batch || !searchResults) {
      return;
    }

    const exportData = createBatchJsonExport(batch, matchingPathOccurrences, pathMode, searchResults.hasQuery ? searchResults.query : null);
    const filename = buildBatchExportFilename(batch.batchId, 'json', pathMode);
    triggerDownload(serializeBatchJsonExport(exportData), filename, 'application/json');
    setFeedbackMessage(`Exported ${pathMode === 'unique' ? 'unique' : 'matching'} paths as JSON.`);
  }

  function handleExportCsv() {
    if (!batch) {
      return;
    }

    const occurrences = searchBatchPathOccurrences(batch, searchQuery, 'occurrences');
    const rows = createBatchCsvRows(batch.batchId, occurrences);
    const filename = buildBatchExportFilename(batch.batchId, 'csv', 'occurrences');
    triggerDownload(serializeBatchCsv(rows), filename, 'text/csv');
    setFeedbackMessage(`Exported ${formatCount(rows.length)} path${rows.length === 1 ? '' : 's'} as CSV.`);
  }

  const selectedPathCount = matchingPathOccurrences.length;

  const liveAnnouncement = refreshWarning
    ? `${refreshWarning.title}. ${refreshWarning.message}`
    : isAutoRefreshPaused
      ? 'Automatic refresh paused. Use manual refresh to try again.'
      : isLongRunning
        ? 'Batch processing is taking longer than expected.'
        : summary?.batchStatus.normalized === 'processing'
          ? 'Batch details are refreshing automatically while processing continues.'
          : summary?.batchStatus.normalized === 'completed'
            ? 'Batch processing completed.'
            : '';

  if (isInitialLoading && !batch) {
    return (
      <div className="stack-lg">
        <PageHeader title="Batch details" subtitle="Retrieving the latest batch status and extraction results." />
        <DataState kind="loading" title="Loading batch details" description="Requesting the latest batch response from the backend." />
      </div>
    );
  }

  if (!batch && blockingError) {
    return (
      <div className="stack-lg">
        <PageHeader
          title="Batch details"
          subtitle="Detailed batch inspection is available once the backend returns a valid batch response."
          actions={
            <div className="page-header-actions">
              <Button variant="secondary" onClick={refresh}>Retry</Button>
              <Button variant="secondary" onClick={onNavigateToUpload}>Submit another batch</Button>
            </div>
          }
        />

        <DataState kind="error" title={blockingError.title} description={blockingError.message} />
        {blockingError.details.length > 0 && (
          <SectionCard title="Technical details" subtitle="Useful for troubleshooting retrieval failures.">
            <ul className="plain-list">
              {blockingError.details.map((detail) => (
                <li key={detail}>{detail}</li>
              ))}
            </ul>
          </SectionCard>
        )}
      </div>
    );
  }

  if (!batch || !summary) {
    return null;
  }

  return (
    <div className="stack-lg">
      <div className="visually-hidden" role="status" aria-live="polite" aria-atomic="true">
        {liveAnnouncement}
      </div>

      <PageHeader
        title="Batch details"
        subtitle="Inspect payloads, extracted property paths, diagnostics, and refresh status using the live batch retrieval contract."
        actions={
          <div className="page-header-actions">
            <Button variant="secondary" onClick={refresh} disabled={isRefreshing} aria-label="Refresh batch details">
              {isRefreshing ? 'Refreshing…' : 'Refresh'}
            </Button>
            <Button variant="secondary" onClick={onNavigateToUpload}>Submit another batch</Button>
          </div>
        }
      />

      {refreshWarning && (
        <section className="callout callout-warning" aria-label="Refresh warning">
          <h3>{refreshWarning.title}</h3>
          <p>{refreshWarning.message}</p>
          {refreshWarning.details.length > 0 && (
            <details>
              <summary>Technical details</summary>
              <ul className="plain-list">
                {refreshWarning.details.map((detail) => (
                  <li key={detail}>{detail}</li>
                ))}
              </ul>
            </details>
          )}
        </section>
      )}

      {batch.responseWarnings.length > 0 && (
        <section className="callout callout-neutral" aria-label="Response warnings">
          <h3>Some response fields were unavailable</h3>
          <p>Available batch details are shown below. Unavailable or malformed optional fields were omitted safely.</p>
          <details>
            <summary>Technical details</summary>
            <ul className="plain-list">
              {batch.responseWarnings.map((warning) => (
                <li key={warning}>{warning}</li>
              ))}
            </ul>
          </details>
        </section>
      )}

      {isLongRunning && (
        <section className="callout callout-info" aria-label="Long running batch notice">
          <h3>Taking longer than expected</h3>
          <p>This batch is still processing after {formatDuration(currentElapsedMs)}. The backend status is preserved while polling continues.</p>
        </section>
      )}

      {isAutoRefreshPaused && (
        <section className="callout callout-warning" aria-label="Automatic refresh paused">
          <h3>Automatic refresh paused</h3>
          <p>Automatic refresh stopped after repeated transient failures. Use manual refresh to try again.</p>
        </section>
      )}

      <SectionCard title="Search, copy, and export" subtitle="Search across extracted property paths and diagnostics using the currently loaded batch response.">
        <div className="stack-sm">
          <label className="field stack-xs">
            <span>Search loaded results</span>
            <input
              type="search"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search by path, payload, document, status, or diagnostic text"
            />
          </label>

          <div className="button-row" role="group" aria-label="Path matching mode">
            <Button variant={pathMode === 'occurrences' ? 'primary' : 'secondary'} onClick={() => setPathMode('occurrences')}>
              Path occurrences
            </Button>
            <Button variant={pathMode === 'unique' ? 'primary' : 'secondary'} onClick={() => setPathMode('unique')}>
              Unique paths
            </Button>
          </div>

          <div className="button-row">
            <Button variant="secondary" onClick={() => void handleCopyMatchingPaths()} disabled={selectedPathCount === 0}>
              Copy matching paths
            </Button>
            <Button variant="secondary" onClick={() => void handleCopyDiagnosticMatches()} disabled={matchingDiagnosticEntries.length === 0}>
              Copy matching diagnostics
            </Button>
            <Button variant="secondary" onClick={handleExportCsv} disabled={searchResults?.pathOccurrences.length === 0}>
              Export CSV (all occurrences)
            </Button>
            <Button variant="secondary" onClick={handleExportJson} disabled={selectedPathCount === 0}>
              Export JSON
            </Button>
          </div>

          <dl className="metric-list" aria-label="Search summary">
            <div>
              <dt>Matching paths</dt>
              <dd>{formatCount(selectedPathCount)}</dd>
            </div>
            <div>
              <dt>Matching diagnostics</dt>
              <dd>{formatCount(matchingDiagnosticEntries.length)}</dd>
            </div>
          </dl>

          {feedbackMessage && <p className="batch-muted" role="status">{feedbackMessage}</p>}

          {searchResults && searchResults.hasQuery && selectedPathCount === 0 && matchingDiagnosticEntries.length === 0 ? (
            <DataState kind="empty" title="No matches found" description="Try a different search term or clear the query to browse all results." />
          ) : null}

          {selectedPathCount > 0 && (
            <details className="diagnostics-panel" open={searchQuery.trim().length === 0}>
              <summary>{selectedPathCount} path{selectedPathCount === 1 ? '' : 's'} available for the current view</summary>
              <ul className="plain-list diagnostics-list" aria-label="Matching property paths">
                {matchingPathOccurrences.map((match) => (
                  <li key={match.key}>
                    <strong>{match.propertyPath}</strong>
                    <div className="batch-muted">{describeSearchMatch(match)}</div>
                  </li>
                ))}
              </ul>
            </details>
          )}

          {matchingDiagnosticEntries.length > 0 && (
            <details className="diagnostics-panel">
              <summary>{matchingDiagnosticEntries.length} diagnostic{matchingDiagnosticEntries.length === 1 ? '' : 's'} match the current search</summary>
              <ul className="plain-list diagnostics-list" aria-label="Matching diagnostics">
                {matchingDiagnosticEntries.map((entry) => (
                  <li key={entry.key}>
                    <strong>{entry.code}</strong>
                    <div>{entry.message}</div>
                    <div className="batch-muted">{describeDiagnosticMatch(entry)}</div>
                  </li>
                ))}
              </ul>
            </details>
          )}
        </div>
      </SectionCard>

      <SectionCard title="Batch summary" subtitle="Backend status is shown separately from derived document outcomes.">
        <dl className="metric-list" aria-label="Batch details summary">
          <div>
            <dt>Batch ID</dt>
            <dd>{batch.batchId}</dd>
          </div>
          <div>
            <dt>Backend status</dt>
            <dd><StatusBadge status={summary.batchStatus.label} tone={summary.batchStatus.tone} /></dd>
          </div>
          <div>
            <dt>Derived outcome</dt>
            <dd>{summary.derivedOutcome ? <StatusBadge status={summary.derivedOutcome.label} tone={summary.derivedOutcome.tone} /> : 'Unavailable'}</dd>
          </div>
          <div>
            <dt>Created</dt>
            <dd>{formatTimestamp(batch.createdUtc)}</dd>
          </div>
          <div>
            <dt>Completed</dt>
            <dd>{formatTimestamp(batch.completedUtc)}</dd>
          </div>
          <div>
            <dt>Elapsed</dt>
            <dd>{formatDuration(summary.elapsedMs ?? currentElapsedMs)}</dd>
          </div>
          <div>
            <dt>Last updated</dt>
            <dd>{formatTimestamp(lastUpdatedAt)}</dd>
          </div>
          <div>
            <dt>Payloads</dt>
            <dd>{formatCount(summary.payloadCount)}</dd>
          </div>
          <div>
            <dt>Documents</dt>
            <dd>{formatCount(summary.documentCount)}</dd>
          </div>
          <div>
            <dt>Successful or recovered</dt>
            <dd>{formatCount(summary.successfulDocumentCount)}</dd>
          </div>
          <div>
            <dt>Failed</dt>
            <dd>{formatCount(summary.failedDocumentCount)}</dd>
          </div>
          <div>
            <dt>Property paths</dt>
            <dd>{formatCount(summary.propertyPathCount)}</dd>
          </div>
        </dl>
      </SectionCard>

      <SectionCard title="Payloads and documents" subtitle="Payload = one uploaded file. Document = one extracted JSON object/array found in that payload.">
        {batch.payloads.length === 0 ? (
          <DataState
            kind="empty"
            title="No payload results available"
            description="The backend returned no payload results for this batch. Refresh later if processing is still underway."
          />
        ) : (
          <div className="stack-sm">
            {batch.payloads.map((payload, payloadOrder) => (
              <PayloadGroup
                key={payload.key}
                payload={payload}
                order={payloadOrder}
                onCopyPropertyPath={(propertyPath) => void copyPropertyPath(propertyPath)}
              />
            ))}
          </div>
        )}
      </SectionCard>
    </div>
  );
}
