const defaultBatchDetailsConfig = {
  pollIntervalMs: 3_000,
  requestTimeoutMs: 10_000,
  longRunningThresholdMs: 15_000,
  retryDelaysMs: [2_000, 5_000, 10_000],
} as const;

function toPositiveInt(value: string | undefined, fallback: number): number {
  const parsed = Number.parseInt(value ?? '', 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

export const batchDetailsConfig = {
  pollIntervalMs: toPositiveInt(import.meta.env.VITE_BATCH_POLL_INTERVAL_MS, defaultBatchDetailsConfig.pollIntervalMs),
  requestTimeoutMs: toPositiveInt(import.meta.env.VITE_BATCH_REQUEST_TIMEOUT_MS, defaultBatchDetailsConfig.requestTimeoutMs),
  longRunningThresholdMs: toPositiveInt(
    import.meta.env.VITE_BATCH_LONG_RUNNING_THRESHOLD_MS,
    defaultBatchDetailsConfig.longRunningThresholdMs,
  ),
  retryDelaysMs: defaultBatchDetailsConfig.retryDelaysMs,
} as const;
