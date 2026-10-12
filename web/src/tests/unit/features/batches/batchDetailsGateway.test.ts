import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../../gateway/apiClient';
import { fetchBatchDetails } from '../../../../features/batches/gateway/batchDetailsGateway';

describe('fetchBatchDetails', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('normalizes a valid batch details response', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          batchId: 'batch-123',
          status: 'Completed',
          createdUtc: '2026-10-12T12:00:00Z',
          completedUtc: '2026-10-12T12:00:05Z',
          payloads: [],
        }),
        {
          status: 200,
          headers: {
            'Content-Type': 'application/json',
          },
        },
      ),
    );

    const result = await fetchBatchDetails('batch-123');

    expect(result.statusCode).toBe(200);
    expect(result.data.batchId).toBe('batch-123');
    expect(result.data.status).toBe('Completed');
  });

  it('throws ApiError when successful batch response is malformed', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ status: 'Completed', payloads: [] }), {
        status: 200,
        headers: {
          'Content-Type': 'application/json',
        },
      }),
    );

    await expect(fetchBatchDetails('batch-123')).rejects.toMatchObject({
      name: 'ApiError',
      statusCode: 200,
      code: 'INVALID_BATCH_RESPONSE',
    } satisfies Partial<ApiError>);
  });

  it('throws ApiError when the response body is not valid JSON', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response('not-json', {
        status: 200,
        headers: {
          'Content-Type': 'application/json',
        },
      }),
    );

    await expect(fetchBatchDetails('batch-123')).rejects.toMatchObject({
      name: 'ApiError',
      statusCode: 200,
      code: 'INVALID_BATCH_RESPONSE',
    } satisfies Partial<ApiError>);
  });
});
