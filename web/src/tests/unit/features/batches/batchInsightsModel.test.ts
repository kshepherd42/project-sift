import { describe, expect, it } from 'vitest';
import { normalizeBatchDetailsResponse } from '../../../../features/batches/batchDetailsModel';
import {
  collectBatchPathOccurrences,
  collectUniqueBatchPathOccurrences,
  describeDiagnosticMatch,
  describeSearchMatch,
  searchBatchDetails,
  searchBatchPathOccurrences,
} from '../../../../features/batches/batchInsightsModel';

describe('batchInsightsModel', () => {
  const batch = normalizeBatchDetailsResponse({
    batchId: 'batch-999',
    status: 'Completed',
    payloads: [
      {
        payloadIndex: 0,
        payloadName: 'first.bin',
        diagnostics: [{ code: 'PAYLOAD_NOTE', message: 'Payload warning.' }],
        documents: [
          {
            documentIndex: 0,
            documentName: 'order',
            type: 'Object',
            status: 'Successful',
            propertyPaths: [{ value: 'customer.id' }, { value: 'customer.name' }],
            diagnostics: [{ code: 'DOC_WARN', message: 'Document warning.' }],
          },
          {
            documentIndex: 1,
            documentName: 'repeat',
            type: 'Object',
            status: 'Successful',
            propertyPaths: [{ value: 'customer.id' }],
            diagnostics: [],
          },
        ],
      },
    ],
    responseWarnings: ['Batch warning.'],
  });

  it('collects property path occurrences and preserves duplicates', () => {
    const occurrences = collectBatchPathOccurrences(batch);

    expect(occurrences).toHaveLength(3);
    expect(occurrences.map((entry) => entry.propertyPath)).toEqual(['customer.id', 'customer.name', 'customer.id']);
  });

  it('collects unique property paths in first-seen order', () => {
    const uniqueOccurrences = collectUniqueBatchPathOccurrences(batch);

    expect(uniqueOccurrences).toHaveLength(2);
    expect(uniqueOccurrences.map((entry) => entry.propertyPath)).toEqual(['customer.id', 'customer.name']);
  });

  it('filters paths and diagnostics by the search query', () => {
    const result = searchBatchDetails(batch, 'customer');

    expect(result.hasQuery).toBe(true);
    expect(result.pathOccurrences).toHaveLength(3);
    expect(result.diagnosticEntries).toHaveLength(0);

    const pathMatches = searchBatchPathOccurrences(batch, 'name', 'occurrences');
    expect(pathMatches).toHaveLength(1);
    expect(pathMatches[0].propertyPath).toBe('customer.name');

    const diagnosticResult = searchBatchDetails(batch, 'warning');
    expect(diagnosticResult.diagnosticEntries).toHaveLength(2);

    const firstPathMatch = searchBatchPathOccurrences(batch, 'customer', 'occurrences')[0];
    expect(describeSearchMatch(firstPathMatch)).toContain('customer.id');
    expect(describeDiagnosticMatch(diagnosticResult.diagnosticEntries[0])).toContain('batch-999');
  });
});
