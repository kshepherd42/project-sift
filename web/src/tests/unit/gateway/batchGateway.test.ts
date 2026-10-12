import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../gateway/apiClient';
import { submitBatch } from '../../../features/upload/gateway/batchGateway';

describe('submitBatch', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('submits all selected files as multipart field files', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ batchId: 'batch-123', resourceUrl: '/v1/batches/batch-123' }), {
        status: 202,
        headers: {
          'Content-Type': 'application/json',
        },
      }),
    );

    const first = new File(['first'], 'a.bin');
    const second = new File(['second'], 'b.bin');

    await submitBatch([first, second]);

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [, requestInit] = fetchMock.mock.calls[0];

    expect(requestInit?.method).toBe('POST');
    expect(requestInit?.body).toBeInstanceOf(FormData);

    const formData = requestInit?.body as FormData;
    const files = formData.getAll('files');

    expect(files).toHaveLength(2);
    expect(files[0]).toBeInstanceOf(File);
    expect(files[1]).toBeInstanceOf(File);
    expect((files[0] as File).name).toBe('a.bin');
    expect((files[1] as File).name).toBe('b.bin');
    expect((files[0] as File).size).toBe(first.size);
    expect((files[1] as File).size).toBe(second.size);
  });

  it('throws ApiError when successful response contains invalid batch identifier payload', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ batchId: '   ', resourceUrl: '/v1/batches/batch-123' }), {
        status: 202,
        headers: {
          'Content-Type': 'application/json',
        },
      }),
    );

    await expect(submitBatch([new File(['first'], 'a.bin')])).rejects.toMatchObject({
      name: 'ApiError',
      statusCode: 202,
      code: 'INVALID_ACCEPTED_RESPONSE',
    } satisfies Partial<ApiError>);
  });

  it('throws ApiError when response body is malformed json', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response('not-json', {
        status: 202,
        headers: {
          'Content-Type': 'application/json',
        },
      }),
    );

    await expect(submitBatch([new File(['first'], 'a.bin')])).rejects.toMatchObject({
      name: 'ApiError',
      statusCode: 202,
      message: 'The API response was not valid JSON.',
    } satisfies Partial<ApiError>);
  });
});
