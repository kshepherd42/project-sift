import type { BatchDetails, BatchDiagnostic } from './batchDetailsModel';

export type BatchPathOccurrence = {
  key: string;
  batchId: string;
  batchStatus: string;
  payloadIndex: number | null;
  payloadName: string | null;
  documentIndex: number | null;
  documentName: string | null;
  documentStatus: string;
  propertyPath: string;
};

export type BatchDiagnosticEntry = {
  key: string;
  batchId: string;
  batchStatus: string;
  payloadIndex: number | null;
  payloadName: string | null;
  documentIndex: number | null;
  documentName: string | null;
  documentStatus: string | null;
  code: string;
  message: string;
  position: number | null;
  level: 'batch' | 'payload' | 'document';
};

export type BatchInsightSearchResult = {
  query: string;
  normalizedQuery: string;
  hasQuery: boolean;
  pathOccurrences: BatchPathOccurrence[];
  diagnosticEntries: BatchDiagnosticEntry[];
};

export type BatchPathMode = 'occurrences' | 'unique';

function normalizeQuery(query: string): string {
  return query.trim().toLowerCase();
}

function includesIgnoreCase(source: string | null | undefined, query: string): boolean {
  if (!source || query.length === 0) {
    return query.length === 0;
  }

  return source.toLowerCase().includes(query);
}

function isSearchableDiagnostic(diagnostic: BatchDiagnostic, query: string): boolean {
  return includesIgnoreCase(diagnostic.code, query) || includesIgnoreCase(diagnostic.message, query);
}

function collectPathOccurrences(batch: BatchDetails): BatchPathOccurrence[] {
  const occurrences: BatchPathOccurrence[] = [];

  for (const payload of batch.payloads) {
    for (const document of payload.documents) {
      for (const [pathIndex, propertyPath] of document.propertyPaths.entries()) {
        occurrences.push({
          key: `${payload.key}-${document.key}-path-${pathIndex}`,
          batchId: batch.batchId,
          batchStatus: batch.status,
          payloadIndex: payload.payloadIndex,
          payloadName: payload.payloadName,
          documentIndex: document.documentIndex,
          documentName: document.documentName,
          documentStatus: document.status,
          propertyPath,
        });
      }
    }
  }

  return occurrences;
}

function collectDiagnosticEntries(batch: BatchDetails): BatchDiagnosticEntry[] {
  const entries: BatchDiagnosticEntry[] = [];

  batch.responseWarnings.forEach((warning, warningIndex) => {
    entries.push({
      key: `batch-warning-${warningIndex}`,
      batchId: batch.batchId,
      batchStatus: batch.status,
      payloadIndex: null,
      payloadName: null,
      documentIndex: null,
      documentName: null,
      documentStatus: null,
      code: 'BATCH_WARNING',
      message: warning,
      position: null,
      level: 'batch',
    });
  });

  for (const payload of batch.payloads) {
    payload.diagnostics.forEach((diagnostic, diagnosticIndex) => {
      entries.push({
        key: `payload-${payload.key}-diagnostic-${diagnosticIndex}`,
        batchId: batch.batchId,
        batchStatus: batch.status,
        payloadIndex: payload.payloadIndex,
        payloadName: payload.payloadName,
        documentIndex: null,
        documentName: null,
        documentStatus: null,
        code: diagnostic.code,
        message: diagnostic.message,
        position: diagnostic.position,
        level: 'payload',
      });
    });

    for (const document of payload.documents) {
      document.diagnostics.forEach((diagnostic, diagnosticIndex) => {
        entries.push({
          key: `document-${document.key}-diagnostic-${diagnosticIndex}`,
          batchId: batch.batchId,
          batchStatus: batch.status,
          payloadIndex: payload.payloadIndex,
          payloadName: payload.payloadName,
          documentIndex: document.documentIndex,
          documentName: document.documentName,
          documentStatus: document.status,
          code: diagnostic.code,
          message: diagnostic.message,
          position: diagnostic.position,
          level: 'document',
        });
      });
    }
  }

  return entries;
}

export function collectBatchPathOccurrences(batch: BatchDetails): BatchPathOccurrence[] {
  return collectPathOccurrences(batch);
}

export function collectUniqueBatchPathOccurrences(batch: BatchDetails): BatchPathOccurrence[] {
  const unique = new Map<string, BatchPathOccurrence>();

  for (const occurrence of collectPathOccurrences(batch)) {
    if (!unique.has(occurrence.propertyPath)) {
      unique.set(occurrence.propertyPath, occurrence);
    }
  }

  return Array.from(unique.values());
}

export function searchBatchDetails(batch: BatchDetails, query: string): BatchInsightSearchResult {
  const normalizedQuery = normalizeQuery(query);
  const hasQuery = normalizedQuery.length > 0;
  const pathOccurrences = collectPathOccurrences(batch).filter((occurrence) => {
    if (!hasQuery) {
      return true;
    }

    return (
      includesIgnoreCase(occurrence.batchId, normalizedQuery)
      || includesIgnoreCase(occurrence.payloadName, normalizedQuery)
      || includesIgnoreCase(occurrence.documentName, normalizedQuery)
      || includesIgnoreCase(occurrence.documentStatus, normalizedQuery)
      || includesIgnoreCase(occurrence.propertyPath, normalizedQuery)
    );
  });

  const diagnosticEntries = collectDiagnosticEntries(batch).filter((entry) => {
    if (!hasQuery) {
      return true;
    }

    return (
      includesIgnoreCase(entry.batchId, normalizedQuery)
      || includesIgnoreCase(entry.payloadName, normalizedQuery)
      || includesIgnoreCase(entry.documentName, normalizedQuery)
      || includesIgnoreCase(entry.documentStatus, normalizedQuery)
      || isSearchableDiagnostic(entry, normalizedQuery)
    );
  });

  return {
    query,
    normalizedQuery,
    hasQuery,
    pathOccurrences,
    diagnosticEntries,
  };
}

export function searchBatchPathOccurrences(batch: BatchDetails, query: string, mode: BatchPathMode): BatchPathOccurrence[] {
  const matches = searchBatchDetails(batch, query).pathOccurrences;

  if (mode === 'occurrences') {
    return matches;
  }

  const seen = new Set<string>();
  const uniqueMatches: BatchPathOccurrence[] = [];
  for (const match of matches) {
    if (seen.has(match.propertyPath)) {
      continue;
    }

    seen.add(match.propertyPath);
    uniqueMatches.push(match);
  }

  return uniqueMatches;
}

export function describeSearchMatch(match: BatchPathOccurrence): string {
  const payloadLabel = match.payloadName ?? `Payload ${match.payloadIndex ?? '?'}`;
  const documentLabel = match.documentName ?? `Document ${match.documentIndex ?? '?'}`;
  return `${payloadLabel} · ${documentLabel} · ${match.propertyPath}`;
}

export function describeDiagnosticMatch(entry: BatchDiagnosticEntry): string {
  const scopeParts = [entry.batchId];

  if (entry.payloadName) {
    scopeParts.push(entry.payloadName);
  }

  if (entry.documentName) {
    scopeParts.push(entry.documentName);
  }

  return `${scopeParts.join(' · ')} · ${entry.message}`;
}
