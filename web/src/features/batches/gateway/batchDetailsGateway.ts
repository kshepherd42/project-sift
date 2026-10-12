import { ApiError, buildApiUrl, type ApiResult } from '../../../gateway/apiClient';
import { normalizeBatchDetailsResponse, type BatchDetails } from '../batchDetailsModel';

type ApiErrorResponse = {
  code?: string;
  message?: string;
  batchId?: string;
  details?: string[];
};

function parseJson(text: string, statusCode: number): unknown {
  try {
    return JSON.parse(text) as unknown;
  } catch {
    throw new ApiError('The API response was not valid JSON.', statusCode, {
      code: 'INVALID_BATCH_RESPONSE',
    });
  }
}

export async function fetchBatchDetails(batchId: string, signal?: AbortSignal): Promise<ApiResult<BatchDetails>> {
  const response = await fetch(buildApiUrl(`/v1/batches/${encodeURIComponent(batchId)}`), {
    method: 'GET',
    headers: {
      Accept: 'application/json',
    },
    signal,
  });

  const rawText = await response.text();
  const parsed = rawText ? parseJson(rawText, response.status) : null;

  if (!response.ok) {
    const errorPayload = (parsed ?? {}) as ApiErrorResponse;
    throw new ApiError(errorPayload.message ?? `Request failed with status ${response.status}.`, response.status, {
      code: errorPayload.code,
      batchId: errorPayload.batchId,
      details: Array.isArray(errorPayload.details) ? errorPayload.details : undefined,
    });
  }

  try {
    return {
      statusCode: response.status,
      data: normalizeBatchDetailsResponse(parsed),
    };
  } catch (error) {
    throw new ApiError('The batch details response was malformed.', response.status, {
      code: 'INVALID_BATCH_RESPONSE',
      batchId,
      details: [error instanceof Error ? error.message : 'Unknown normalization error'],
    });
  }
}
