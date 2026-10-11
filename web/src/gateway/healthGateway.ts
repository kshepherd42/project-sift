import { getJson, type ApiResult } from './apiClient';

export type HealthResponse = {
  status: string;
};

export function fetchHealth(): Promise<ApiResult<HealthResponse>> {
  return getJson<HealthResponse>('/api/health');
}
