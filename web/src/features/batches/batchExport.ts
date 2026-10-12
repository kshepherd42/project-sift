import type { BatchDetails } from './batchDetailsModel';
import type { BatchPathOccurrence } from './batchInsightsModel';

export type BatchExportScope = 'occurrences' | 'unique';

export type BatchJsonExport = {
  batchId: string;
  generatedAtUtc: string;
  scope: BatchExportScope;
  query: string | null;
  payloads: Array<{
    payloadIndex: number | null;
    payloadName: string | null;
    documents: Array<{
      documentIndex: number | null;
      documentName: string | null;
      documentStatus: string;
      propertyPaths: string[];
    }>;
  }>;
};

export type BatchCsvExportRow = {
  batchId: string;
  payloadIndex: string;
  payloadName: string;
  documentIndex: string;
  documentName: string;
  documentStatus: string;
  propertyPath: string;
};

function quoteCsvField(value: string): string {
  return `"${value.split('"').join('""')}"`;
}

function toCsvCell(value: string | number | null | undefined): string {
  return quoteCsvField(value === null || value === undefined ? '' : String(value));
}

export function buildBatchExportFilename(batchId: string, kind: 'json' | 'csv', scope: BatchExportScope): string {
  const safeBatchId = batchId.trim().replace(/[^a-z0-9_-]+/gi, '-').replace(/^-+|-+$/g, '') || 'batch';
  return `projectsift-${safeBatchId}-${scope}.${kind}`;
}

export function createBatchJsonExport(
  batch: BatchDetails,
  occurrences: BatchPathOccurrence[],
  scope: BatchExportScope,
  query: string | null,
): BatchJsonExport {
  const grouped = new Map<string, BatchJsonExport['payloads'][number]>();

  for (const occurrence of occurrences) {
    const payloadKey = `${occurrence.payloadIndex ?? 'null'}:${occurrence.payloadName ?? ''}`;
    const existingPayload = grouped.get(payloadKey);

    const document = {
      documentIndex: occurrence.documentIndex,
      documentName: occurrence.documentName,
      documentStatus: occurrence.documentStatus,
      propertyPaths: [occurrence.propertyPath],
    };

    if (!existingPayload) {
      grouped.set(payloadKey, {
        payloadIndex: occurrence.payloadIndex,
        payloadName: occurrence.payloadName,
        documents: [document],
      });
      continue;
    }

    const documentKey = `${occurrence.documentIndex ?? 'null'}:${occurrence.documentName ?? ''}:${occurrence.documentStatus}`;
    const existingDocument = existingPayload.documents.find(
      (entry) => `${entry.documentIndex ?? 'null'}:${entry.documentName ?? ''}:${entry.documentStatus}` === documentKey,
    );

    if (existingDocument) {
      if (scope === 'occurrences') {
        existingDocument.propertyPaths.push(occurrence.propertyPath);
      } else if (existingDocument.propertyPaths[existingDocument.propertyPaths.length - 1] !== occurrence.propertyPath) {
        existingDocument.propertyPaths.push(occurrence.propertyPath);
      }
      continue;
    }

    existingPayload.documents.push(document);
  }

  return {
    batchId: batch.batchId,
    generatedAtUtc: new Date().toISOString(),
    scope,
    query,
    payloads: Array.from(grouped.values()),
  };
}

export function serializeBatchJsonExport(exportData: BatchJsonExport): string {
  return JSON.stringify(exportData, null, 2);
}

export function createBatchCsvRows(batchId: string, occurrences: BatchPathOccurrence[]): BatchCsvExportRow[] {
  return occurrences.map((occurrence) => ({
    batchId,
    payloadIndex: occurrence.payloadIndex === null ? '' : String(occurrence.payloadIndex),
    payloadName: occurrence.payloadName ?? '',
    documentIndex: occurrence.documentIndex === null ? '' : String(occurrence.documentIndex),
    documentName: occurrence.documentName ?? '',
    documentStatus: occurrence.documentStatus,
    propertyPath: occurrence.propertyPath,
  }));
}

export function serializeBatchCsv(rows: BatchCsvExportRow[]): string {
  const headers = ['batchId', 'payloadIndex', 'payloadName', 'documentIndex', 'documentName', 'documentStatus', 'propertyPath'];
  const lines = [headers.map(quoteCsvField).join(',')];

  for (const row of rows) {
    lines.push([
      row.batchId,
      row.payloadIndex,
      row.payloadName,
      row.documentIndex,
      row.documentName,
      row.documentStatus,
      row.propertyPath,
    ].map(toCsvCell).join(','));
  }

  return `${lines.join('\r\n')}\r\n`;
}

export async function copyTextToClipboard(text: string): Promise<void> {
  if (!navigator.clipboard?.writeText) {
    throw new Error('Clipboard access is unavailable in this browser context.');
  }

  await navigator.clipboard.writeText(text);
}

export function triggerDownload(content: string, filename: string, mimeType: string): void {
  const blob = new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.rel = 'noopener';
  anchor.click();
  URL.revokeObjectURL(url);
}
