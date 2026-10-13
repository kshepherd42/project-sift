import { afterEach, describe, expect, it, vi } from 'vitest';
import { normalizeBatchDetailsResponse } from '../../../../features/batches/batchDetailsModel';
import {
  buildBatchExportFilename,
  createBatchCsvRows,
  createBatchJsonExport,
  serializeBatchCsv,
  serializeBatchJsonExport,
  copyTextToClipboard,
} from '../../../../features/batches/batchExport';

describe('batchExport', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

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

  it('preserves duplicate CSV occurrences and escapes line breaks and Unicode', () => {
    const occurrence = {
      key: 'path-1',
      batchId: batch.batchId,
      batchStatus: batch.status,
      payloadIndex: 0,
      payloadName: 'café\r\nsource.bin',
      documentIndex: 0,
      documentName: 'doc',
      documentStatus: 'Successful',
      propertyPath: 'customer.id',
    };
    const rows = createBatchCsvRows(batch.batchId, [occurrence, { ...occurrence, key: 'path-2' }]);
    const csv = serializeBatchCsv(rows);

    expect(rows).toHaveLength(2);
    expect(csv.match(/"customer.id"/g)).toHaveLength(2);
    expect(csv).toContain('"café\r\nsource.bin"');
    expect(csv).toContain('\r\n');
  });

  it('resolves clipboard success and failure without claiming success on rejection', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    vi.stubGlobal('navigator', { clipboard: { writeText } });

    await expect(copyTextToClipboard('customer.id')).resolves.toBeUndefined();
    expect(writeText).toHaveBeenCalledWith('customer.id');

    writeText.mockRejectedValueOnce(new Error('Permission denied.'));
    await expect(copyTextToClipboard('customer.id')).rejects.toThrow('Permission denied.');
  });
});
