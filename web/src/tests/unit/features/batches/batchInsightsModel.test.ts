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

  it('preserves repeated paths with unique occurrence keys', () => {
    const repeatedPathBatch = normalizeBatchDetailsResponse({
      batchId: 'batch-repeated-path',
      status: 'Completed',
      payloads: [{
        documents: [{
          propertyPaths: [{ value: 'item.id' }, { value: 'item.id' }],
        }],
      }],
    });

    const occurrences = collectBatchPathOccurrences(repeatedPathBatch);

    expect(occurrences.map((entry) => entry.propertyPath)).toEqual(['item.id', 'item.id']);
    expect(new Set(occurrences.map((entry) => entry.key)).size).toBe(2);
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

  it('searches names, statuses, nested paths, and diagnostic text case-insensitively', () => {
    const nestedBatch = normalizeBatchDetailsResponse({
      batchId: 'batch-nested-search',
      status: 'Completed',
      payloads: [{
        payloadName: 'Inventory.BIN',
        documents: [{
          documentName: 'Warehouse Order',
          status: 'Recovered',
          propertyPaths: [{ value: 'warehouse.zone' }],
          diagnostics: [{ code: 'RECOVERY_NOTE', message: 'Recovered from truncated input.' }],
        }],
      }],
    });

    expect(searchBatchDetails(nestedBatch, 'WAREHOUSE').pathOccurrences).toHaveLength(1);
    expect(searchBatchDetails(nestedBatch, 'inventory.bin').pathOccurrences).toHaveLength(1);
    expect(searchBatchDetails(nestedBatch, 'recovered').pathOccurrences).toHaveLength(1);
    expect(searchBatchDetails(nestedBatch, 'TRUNCATED').diagnosticEntries).toHaveLength(1);
    expect(searchBatchDetails(nestedBatch, '   ').pathOccurrences).toHaveLength(1);
  });
});
