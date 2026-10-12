export type StatusTone = 'success' | 'warning' | 'error' | 'info' | 'neutral';

type BatchStatusKind = 'processing' | 'completed' | 'unknown';
type DocumentStatusKind = 'successful' | 'recovered' | 'failed' | 'unknown';

export type BatchStatusInfo = {
  label: string;
  normalized: BatchStatusKind;
  tone: StatusTone;
  isKnown: boolean;
  isTerminal: boolean;
};

export type DocumentStatusInfo = {
  label: string;
  normalized: DocumentStatusKind;
  tone: StatusTone;
  isKnown: boolean;
  countsAsSuccess: boolean;
  countsAsFailure: boolean;
  countsAsRecovered: boolean;
};

export type DerivedOutcome = {
  label: string;
  tone: StatusTone;
  isDerived: true;
};

function normalizeToken(value: string | null | undefined): string {
  return value?.trim().toLowerCase() ?? '';
}

function preserveLabel(value: string | null | undefined, fallback: string): string {
  return value?.trim() || fallback;
}

export function interpretBatchStatus(status: string | null | undefined): BatchStatusInfo {
  switch (normalizeToken(status)) {
    case 'processing':
      return { label: 'Processing', normalized: 'processing', tone: 'info', isKnown: true, isTerminal: false };
    case 'completed':
      return { label: 'Completed', normalized: 'completed', tone: 'success', isKnown: true, isTerminal: true };
    default:
      return {
        label: preserveLabel(status, 'Unknown'),
        normalized: 'unknown',
        tone: 'neutral',
        isKnown: false,
        isTerminal: false,
      };
  }
}

export function shouldPollBatchStatus(status: string | null | undefined): boolean {
  return interpretBatchStatus(status).normalized === 'processing';
}

export function interpretDocumentStatus(status: string | null | undefined): DocumentStatusInfo {
  switch (normalizeToken(status)) {
    case 'successful':
      return {
        label: 'Successful',
        normalized: 'successful',
        tone: 'success',
        isKnown: true,
        countsAsSuccess: true,
        countsAsFailure: false,
        countsAsRecovered: false,
      };
    case 'recovered':
      return {
        label: 'Recovered',
        normalized: 'recovered',
        tone: 'warning',
        isKnown: true,
        countsAsSuccess: true,
        countsAsFailure: false,
        countsAsRecovered: true,
      };
    case 'failed':
      return {
        label: 'Failed',
        normalized: 'failed',
        tone: 'error',
        isKnown: true,
        countsAsSuccess: false,
        countsAsFailure: true,
        countsAsRecovered: false,
      };
    default:
      return {
        label: preserveLabel(status, 'Unknown'),
        normalized: 'unknown',
        tone: 'neutral',
        isKnown: false,
        countsAsSuccess: false,
        countsAsFailure: false,
        countsAsRecovered: false,
      };
  }
}

export function interpretDocumentType(type: string | null | undefined): string {
  switch (normalizeToken(type)) {
    case 'object':
      return 'Object';
    case 'array':
      return 'Array';
    case 'unknown':
      return 'Unknown';
    default:
      return preserveLabel(type, 'Unknown');
  }
}

export function deriveBatchOutcome(counts: {
  documentCount: number;
  successfulDocumentCount: number;
  recoveredDocumentCount: number;
  failedDocumentCount: number;
  unknownDocumentCount: number;
}): DerivedOutcome | null {
  if (counts.documentCount === 0) {
    return null;
  }

  if (counts.failedDocumentCount > 0 && counts.successfulDocumentCount > 0) {
    return { label: 'Partial success', tone: 'warning', isDerived: true };
  }

  if (counts.failedDocumentCount > 0 && counts.successfulDocumentCount === 0 && counts.unknownDocumentCount === 0) {
    return { label: 'Failed', tone: 'error', isDerived: true };
  }

  if (counts.unknownDocumentCount > 0) {
    return { label: 'Mixed/unknown', tone: 'neutral', isDerived: true };
  }

  if (counts.recoveredDocumentCount > 0) {
    return { label: 'Recovered', tone: 'warning', isDerived: true };
  }

  if (counts.successfulDocumentCount > 0) {
    return { label: 'Successful', tone: 'success', isDerived: true };
  }

  return null;
}
