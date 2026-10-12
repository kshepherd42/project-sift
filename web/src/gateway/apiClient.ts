const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5050';

export function buildApiUrl(path: string): string {
  return `${apiBaseUrl}${path}`;
}

export type ApiResult<T> = {
  statusCode: number;
  data: T;
};

export class ApiError extends Error {
  readonly statusCode: number;
  readonly code?: string;
  readonly batchId?: string;
  readonly details?: string[];

  constructor(message: string, statusCode: number, options?: { code?: string; batchId?: string; details?: string[] }) {
    super(message);
    this.name = 'ApiError';
    this.statusCode = statusCode;
    this.code = options?.code;
    this.batchId = options?.batchId;
    this.details = options?.details;
  }
}

export async function getJson<T>(path: string): Promise<ApiResult<T>> {
  const response = await fetch(buildApiUrl(path), {
    method: 'GET',
    headers: {
      Accept: 'application/json',
    },
  });

  let data: T | null = null;

  try {
    data = (await response.json()) as T;
  } catch {
    throw new ApiError('The API response was not valid JSON.', response.status);
  }

  if (!response.ok) {
    throw new ApiError(`Request failed with status ${response.status}.`, response.status);
  }

  return {
    statusCode: response.status,
    data,
  };
}
