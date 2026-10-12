import { describe, expect, it } from 'vitest';
import { deriveBatchDetailsSummary, normalizeBatchDetailsResponse } from '../../../../features/batches/batchDetailsModel';

describe('batchDetailsModel', () => {
  it('normalizes missing optional fields and preserves unknown statuses safely', () => {
    const batch = normalizeBatchDetailsResponse({
      batchId: 'batch-123',
      status: 'Completed',
      createdUtc: '2026-10-12T12:00:00Z',
      payloads: [
        {
          payloadIndex: 0,
          payloadName: null,
          documents: [
            {
              documentIndex: 0,
              documentName: null,
              type: 'CustomType',
              status: 'UnexpectedStatus',
              propertyPaths: [{ value: 'customer.id' }, { invalid: true }],
              diagnostics: [{ code: 'CUSTOM', message: 'Something happened.', position: 3 }],
            },
          ],
        },
      ],
    });

    expect(batch.payloads[0].payloadName).toBeNull();
    expect(batch.payloads[0].documents[0].documentName).toBeNull();
    expect(batch.payloads[0].documents[0].status).toBe('UnexpectedStatus');
    expect(batch.payloads[0].documents[0].propertyPaths).toEqual(['customer.id']);
    expect(batch.responseWarnings).not.toHaveLength(0);

    const summary = deriveBatchDetailsSummary(batch);
    expect(summary.unknownDocumentCount).toBe(1);
    expect(summary.propertyPathCount).toBe(1);
    expect(summary.derivedOutcome?.label).toBe('Mixed/unknown');
  });

  it('derives explicit counts and partial-success outcome from child document statuses', () => {
    const batch = normalizeBatchDetailsResponse({
      batchId: 'batch-abc',
      status: 'Completed',
      createdUtc: '2026-10-12T12:00:00Z',
      completedUtc: '2026-10-12T12:00:09Z',
      payloads: [
        {
          payloadIndex: 0,
          payloadName: 'payload.bin',
          diagnostics: [],
          documents: [
            {
              documentIndex: 0,
              documentName: 'order',
              type: 'Object',
              status: 'Successful',
              propertyPaths: [{ value: 'order.id' }],
              diagnostics: [],
            },
            {
              documentIndex: 1,
              documentName: 'diagnostic',
              type: 'Unknown',
              status: 'Failed',
              propertyPaths: [],
              diagnostics: [{ code: 'BAD_JSON', message: 'Invalid JSON.' }],
            },
          ],
        },
      ],
    });

    const summary = deriveBatchDetailsSummary(batch);

    expect(summary.payloadCount).toBe(1);
    expect(summary.documentCount).toBe(2);
    expect(summary.successfulDocumentCount).toBe(1);
    expect(summary.failedDocumentCount).toBe(1);
    expect(summary.propertyPathCount).toBe(1);
    expect(summary.elapsedMs).toBe(9_000);
    expect(summary.derivedOutcome?.label).toBe('Partial success');
  });
});
