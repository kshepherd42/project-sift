const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim();
const apiBaseUrl = configuredApiBaseUrl || (import.meta.env.DEV ? 'http://localhost:5050' : null);

export function buildApiUrl(path: string): string {
  if (!apiBaseUrl) {
    throw new ApiError('API base URL is not configured. Set VITE_API_BASE_URL to the backend origin.', 0, {
      code: 'API_CONFIGURATION_MISSING',
    });
  }

  try {
    const parsedBaseUrl = new URL(apiBaseUrl);
    if (parsedBaseUrl.protocol !== 'http:' && parsedBaseUrl.protocol !== 'https:') {
      throw new Error('Unsupported API base URL protocol.');
    }

    return new URL(path, parsedBaseUrl).toString();
  } catch {
    throw new ApiError('VITE_API_BASE_URL must be a valid absolute HTTP or HTTPS URL.', 0, {
      code: 'API_CONFIGURATION_INVALID',
    });
  }
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
