import { deriveBatchOutcome, interpretBatchStatus, interpretDocumentStatus, interpretDocumentType } from './batchDetailsStatus';

export type BatchDiagnostic = {
  code: string;
  message: string;
  position: number | null;
  key: string;
};

export type BatchDocument = {
  documentIndex: number | null;
  documentName: string | null;
  type: string;
  status: string;
  propertyPaths: string[];
  diagnostics: BatchDiagnostic[];
  key: string;
};

export type BatchPayload = {
  payloadIndex: number | null;
  payloadName: string | null;
  documents: BatchDocument[];
  diagnostics: BatchDiagnostic[];
  key: string;
};

export type BatchDetails = {
  batchId: string;
  status: string;
  payloads: BatchPayload[];
  createdUtc: string | null;
  completedUtc: string | null;
  responseWarnings: string[];
};

export type BatchDetailsSummary = {
  payloadCount: number;
  documentCount: number;
  successfulDocumentCount: number;
  recoveredDocumentCount: number;
  failedDocumentCount: number;
  unknownDocumentCount: number;
  propertyPathCount: number;
  elapsedMs: number | null;
  derivedOutcome: ReturnType<typeof deriveBatchOutcome>;
  batchStatus: ReturnType<typeof interpretBatchStatus>;
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function toNonEmptyString(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : null;
}

function toNullableName(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value : null;
}

function toInteger(value: unknown): number | null {
  return typeof value === 'number' && Number.isInteger(value) ? value : null;
}

function toDateString(value: unknown): string | null {
  if (typeof value !== 'string' || value.trim().length === 0) {
    return null;
  }

  return Number.isNaN(new Date(value).getTime()) ? null : value;
}

function normalizeDiagnostic(value: unknown, keyPrefix: string, index: number, warnings: string[]): BatchDiagnostic | null {
  if (!isRecord(value)) {
    warnings.push(`${keyPrefix} diagnostic ${index + 1} was ignored because it was not an object.`);
    return null;
  }

  return {
    code: toNonEmptyString(value.code) ?? 'UNKNOWN_DIAGNOSTIC',
    message: toNonEmptyString(value.message) ?? 'Diagnostic message unavailable.',
    position: toInteger(value.position),
    key: `${keyPrefix}-diagnostic-${index}`,
  };
}

function normalizePropertyPath(value: unknown, index: number, warnings: string[]): string | null {
  if (!isRecord(value)) {
    warnings.push(`Property path ${index + 1} was ignored because it was not an object.`);
    return null;
  }

  const normalized = toNonEmptyString(value.value);
  if (!normalized) {
    warnings.push(`Property path ${index + 1} was ignored because it did not include a valid value.`);
    return null;
  }

  return normalized;
}

function normalizeDocument(value: unknown, payloadIndex: number, documentOrder: number, warnings: string[]): BatchDocument | null {
  if (!isRecord(value)) {
    warnings.push(`Payload ${payloadIndex + 1} document ${documentOrder + 1} was ignored because it was not an object.`);
    return null;
  }

  const documentWarnings = warnings;
  const diagnostics = Array.isArray(value.diagnostics)
    ? value.diagnostics
        .map((entry, index) => normalizeDiagnostic(entry, `payload-${payloadIndex}-document-${documentOrder}`, index, documentWarnings))
        .filter((entry): entry is BatchDiagnostic => entry !== null)
    : [];

  const propertyPaths = Array.isArray(value.propertyPaths)
    ? value.propertyPaths
        .map((entry, index) => normalizePropertyPath(entry, index, documentWarnings))
        .filter((entry): entry is string => entry !== null)
    : [];

  return {
    documentIndex: toInteger(value.documentIndex),
    documentName: toNullableName(value.documentName),
    type: toNonEmptyString(value.type) ?? 'Unknown',
    status: toNonEmptyString(value.status) ?? 'Unknown',
    propertyPaths,
    diagnostics,
    key: `payload-${payloadIndex}-document-${documentOrder}`,
  };
}

function normalizePayload(value: unknown, payloadOrder: number, warnings: string[]): BatchPayload | null {
  if (!isRecord(value)) {
    warnings.push(`Payload ${payloadOrder + 1} was ignored because it was not an object.`);
    return null;
  }

  const documents = Array.isArray(value.documents)
    ? value.documents
        .map((entry, index) => normalizeDocument(entry, payloadOrder, index, warnings))
        .filter((entry): entry is BatchDocument => entry !== null)
    : [];

  const diagnostics = Array.isArray(value.diagnostics)
    ? value.diagnostics
        .map((entry, index) => normalizeDiagnostic(entry, `payload-${payloadOrder}`, index, warnings))
        .filter((entry): entry is BatchDiagnostic => entry !== null)
    : [];

  return {
    payloadIndex: toInteger(value.payloadIndex),
    payloadName: toNullableName(value.payloadName),
    documents,
    diagnostics,
    key: `payload-${payloadOrder}`,
  };
}

export function normalizeBatchDetailsResponse(value: unknown): BatchDetails {
  if (!isRecord(value)) {
    throw new Error('The batch details response was not an object.');
  }

  const batchId = toNonEmptyString(value.batchId);
  if (!batchId) {
    throw new Error('The batch details response did not include a valid batch identifier.');
  }

  const warnings: string[] = [];
  const payloads = Array.isArray(value.payloads)
    ? value.payloads.map((entry, index) => normalizePayload(entry, index, warnings)).filter((entry): entry is BatchPayload => entry !== null)
    : [];

  if (!Array.isArray(value.payloads)) {
    warnings.push('Payload collection was unavailable in the batch details response.');
  }

  return {
    batchId,
    status: toNonEmptyString(value.status) ?? 'Unknown',
    payloads,
    createdUtc: toDateString(value.createdUtc),
    completedUtc: toDateString(value.completedUtc),
    responseWarnings: warnings,
  };
}

export function deriveBatchDetailsSummary(batch: BatchDetails): BatchDetailsSummary {
  let documentCount = 0;
  let successfulDocumentCount = 0;
  let recoveredDocumentCount = 0;
  let failedDocumentCount = 0;
  let unknownDocumentCount = 0;
  let propertyPathCount = 0;

  for (const payload of batch.payloads) {
    for (const document of payload.documents) {
      documentCount += 1;
      propertyPathCount += document.propertyPaths.length;

      const status = interpretDocumentStatus(document.status);
      if (status.countsAsSuccess) {
        successfulDocumentCount += 1;
      }

      if (status.countsAsRecovered) {
        recoveredDocumentCount += 1;
      }

      if (status.countsAsFailure) {
        failedDocumentCount += 1;
      }

      if (!status.isKnown) {
        unknownDocumentCount += 1;
      }
    }
  }

  const createdTime = batch.createdUtc ? new Date(batch.createdUtc).getTime() : Number.NaN;
  const completedTime = batch.completedUtc ? new Date(batch.completedUtc).getTime() : Number.NaN;
  const elapsedMs = Number.isFinite(createdTime) && Number.isFinite(completedTime) && completedTime >= createdTime
    ? completedTime - createdTime
    : null;

  return {
    payloadCount: batch.payloads.length,
    documentCount,
    successfulDocumentCount,
    recoveredDocumentCount,
    failedDocumentCount,
    unknownDocumentCount,
    propertyPathCount,
    elapsedMs,
    derivedOutcome: deriveBatchOutcome({
      documentCount,
      successfulDocumentCount,
      recoveredDocumentCount,
      failedDocumentCount,
      unknownDocumentCount,
    }),
    batchStatus: interpretBatchStatus(batch.status),
  };
}

export function getPayloadHeading(payload: BatchPayload, order: number): string {
  const position = payload.payloadIndex ?? order;
  return `Payload ${position + 1}`;
}

export function getPayloadNameLabel(payload: BatchPayload): string {
  return payload.payloadName ?? 'Name unavailable';
}

export function getDocumentHeading(document: BatchDocument, order: number): string {
  const position = document.documentIndex ?? order;
  return `Document ${position + 1}`;
}

export function getDocumentNameLabel(document: BatchDocument): string {
  return document.documentName ?? 'Name unavailable';
}

export function getDocumentTypeLabel(document: BatchDocument): string {
  return interpretDocumentType(document.type);
}
