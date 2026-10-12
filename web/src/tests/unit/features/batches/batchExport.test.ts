import { describe, expect, it } from 'vitest';
import { normalizeBatchDetailsResponse } from '../../../../features/batches/batchDetailsModel';
import {
  buildBatchExportFilename,
  createBatchCsvRows,
  createBatchJsonExport,
  serializeBatchCsv,
  serializeBatchJsonExport,
} from '../../../../features/batches/batchExport';

describe('batchExport', () => {
  const batch = normalizeBatchDetailsResponse({
    batchId: 'batch/export-1',
    status: 'Completed',
    payloads: [
      {
        payloadIndex: 0,
        payloadName: 'payload-one.bin',
        documents: [
          {
            documentIndex: 0,
            documentName: 'doc-a',
            type: 'Object',
            status: 'Successful',
            propertyPaths: [{ value: 'customer.id' }, { value: 'customer.name' }],
            diagnostics: [],
          },
        ],
        diagnostics: [],
      },
    ],
  });

  it('builds safe filenames for exports', () => {
    expect(buildBatchExportFilename('batch/export-1', 'json', 'unique')).toBe('projectsift-batch-export-1-unique.json');
  });

  it('serializes json exports with batch and path context', () => {
    const exportData = createBatchJsonExport(batch, [
      {
        key: 'one',
        batchId: batch.batchId,
        batchStatus: batch.status,
        payloadIndex: 0,
        payloadName: 'payload-one.bin',
        documentIndex: 0,
        documentName: 'doc-a',
        documentStatus: 'Successful',
        propertyPath: 'customer.id',
      },
    ], 'unique', 'customer');

    expect(exportData.batchId).toBe(batch.batchId);
    expect(exportData.scope).toBe('unique');
    expect(exportData.query).toBe('customer');
    expect(exportData.payloads).toHaveLength(1);
    expect(serializeBatchJsonExport(exportData)).toContain('customer.id');
  });

  it('serializes csv rows with escaped values', () => {
    const rows = createBatchCsvRows(batch.batchId, [
      {
        key: 'one',
        batchId: batch.batchId,
        batchStatus: batch.status,
        payloadIndex: 0,
        payloadName: 'payload,one.bin',
        documentIndex: 0,
        documentName: 'doc"a',
        documentStatus: 'Successful',
        propertyPath: 'customer.id',
      },
    ]);

    expect(rows).toHaveLength(1);
    expect(serializeBatchCsv(rows)).toContain('"payload,one.bin"');
    expect(serializeBatchCsv(rows)).toContain('"doc""a"');
  });
});
