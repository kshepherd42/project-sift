const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5050';

export type ApiResult<T> = {
  statusCode: number;
  data: T;
};

export class ApiError extends Error {
  readonly statusCode: number;

  constructor(message: string, statusCode: number) {
    super(message);
    this.name = 'ApiError';
    this.statusCode = statusCode;
  }
}

export async function getJson<T>(path: string): Promise<ApiResult<T>> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
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
