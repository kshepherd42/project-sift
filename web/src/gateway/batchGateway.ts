import { ApiError, buildApiUrl, type ApiResult } from './apiClient';

type BatchAcceptedResponse = {
  batchId?: string;
  resourceUrl?: string;
};

type ApiErrorResponse = {
  code?: string;
  message?: string;
  batchId?: string;
  details?: string[];
};

function normalizeBatchAcceptedResponse(value: BatchAcceptedResponse): { batchId: string; resourceUrl: string } {
  const batchId = value.batchId?.trim() ?? '';
  const resourceUrl = value.resourceUrl?.trim() ?? '';

  if (!batchId || !resourceUrl) {
    throw new ApiError('The API response was missing a valid batch identifier.', 202, {
      code: 'INVALID_ACCEPTED_RESPONSE',
    });
  }

  return { batchId, resourceUrl };
}

export async function submitBatch(files: File[], signal?: AbortSignal): Promise<ApiResult<{ batchId: string; resourceUrl: string }>> {
  const formData = new FormData();
  for (const file of files) {
    formData.append('files', file, file.name);
  }

  const response = await fetch(buildApiUrl('/v1/batches'), {
    method: 'POST',
    body: formData,
    signal,
  });

  const rawText = await response.text();
  let parsed: BatchAcceptedResponse | ApiErrorResponse | null = null;

  if (rawText) {
    try {
      parsed = JSON.parse(rawText) as BatchAcceptedResponse | ApiErrorResponse;
    } catch {
      throw new ApiError('The API response was not valid JSON.', response.status);
    }
  }

  if (!response.ok) {
    const errorPayload = parsed as ApiErrorResponse | null;
    throw new ApiError(errorPayload?.message ?? `Request failed with status ${response.status}.`, response.status, {
      code: errorPayload?.code,
      batchId: errorPayload?.batchId,
      details: Array.isArray(errorPayload?.details) ? errorPayload.details : undefined,
    });
  }

  const accepted = normalizeBatchAcceptedResponse((parsed ?? {}) as BatchAcceptedResponse);
  return {
    statusCode: response.status,
    data: accepted,
  };
}
