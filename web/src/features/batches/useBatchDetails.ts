import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ApiError } from '../../gateway/apiClient';
import { batchDetailsConfig } from './batchDetailsConfig';
import { type BatchDetails, deriveBatchDetailsSummary } from './batchDetailsModel';
import { fetchBatchDetails } from './gateway/batchDetailsGateway';
import { shouldPollBatchStatus } from './batchDetailsStatus';

type MessageState = {
  title: string;
  message: string;
  details: string[];
};

type RefreshReason = 'initial' | 'poll' | 'resume' | 'manual';

type UseBatchDetailsResult = {
  batch: BatchDetails | null;
  summary: ReturnType<typeof deriveBatchDetailsSummary> | null;
  blockingError: MessageState | null;
  refreshWarning: MessageState | null;
  isInitialLoading: boolean;
  isRefreshing: boolean;
  isAutoRefreshPaused: boolean;
  lastUpdatedAt: Date | null;
  currentElapsedMs: number | null;
  isLongRunning: boolean;
  refresh: () => void;
};

function isAbortError(error: unknown): error is DOMException {
  return error instanceof DOMException && error.name === 'AbortError';
}

function isTransientApiError(error: unknown): boolean {
  if (isAbortError(error)) {
    return true;
  }

  if (error instanceof ApiError) {
    return error.statusCode === 408 || error.statusCode === 429 || error.statusCode >= 500;
  }

  return error instanceof TypeError;
}

function toMessageState(error: unknown, hasData: boolean, exhaustedRetries: boolean): MessageState {
  if (error instanceof ApiError) {
    if (error.statusCode === 404 || error.code === 'BATCH_NOT_FOUND') {
      return {
        title: 'Batch not found',
        message: 'The requested batch could not be found. Verify the batch identifier and try again.',
        details: [error.message, error.code].filter((value): value is string => !!value),
      };
    }

    if (error.code === 'INVALID_BATCH_RESPONSE') {
      return {
        title: hasData ? 'Unable to refresh batch details' : 'Batch details unavailable',
        message: hasData
          ? 'The latest batch response could not be interpreted. Showing the last successful retrieval.'
          : 'The batch response was malformed and could not be displayed safely.',
        details: [error.message, ...(error.details ?? [])],
      };
    }

    if (isTransientApiError(error)) {
      return {
        title: hasData ? 'Showing last successful retrieval' : 'Unable to load batch details',
        message: exhaustedRetries
          ? 'Automatic refresh paused after repeated transient failures. Retry manually when ready.'
          : 'The backend is temporarily unavailable. Retrying with backoff while preserving the last successful data.',
        details: [error.message, error.code].filter((value): value is string => !!value),
      };
    }

    return {
      title: hasData ? 'Unable to refresh batch details' : 'Batch details unavailable',
      message: error.message,
      details: [error.code, ...(error.details ?? [])].filter((value): value is string => !!value),
    };
  }

  if (isAbortError(error)) {
    return {
      title: hasData ? 'Refresh timed out' : 'Batch details timed out',
      message: hasData
        ? 'The latest refresh timed out. Showing the last successful retrieval.'
        : 'The request timed out before batch details could be retrieved.',
      details: ['Request exceeded the frontend timeout window.'],
    };
  }

  return {
    title: hasData ? 'Unable to refresh batch details' : 'Batch details unavailable',
    message: hasData
      ? 'A transient or unexpected error prevented the latest refresh. Showing the last successful retrieval.'
      : 'An unexpected error prevented batch details from loading.',
    details: [error instanceof Error ? error.message : 'Unknown error'],
  };
}

export function useBatchDetails(batchId: string | null): UseBatchDetailsResult {
  const [batch, setBatch] = useState<BatchDetails | null>(null);
  const [blockingError, setBlockingError] = useState<MessageState | null>(null);
  const [refreshWarning, setRefreshWarning] = useState<MessageState | null>(null);
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isAutoRefreshPaused, setIsAutoRefreshPaused] = useState(false);
  const [lastUpdatedAt, setLastUpdatedAt] = useState<Date | null>(null);
  const [now, setNow] = useState(() => Date.now());

  const batchRef = useRef<BatchDetails | null>(null);
  const timerRef = useRef<number | null>(null);
  const requestContextRef = useRef<{ requestId: number; controller: AbortController; timeoutId: number } | null>(null);
  const requestInFlightRef = useRef(false);
  const retryAttemptRef = useRef(0);
  const requestIdRef = useRef(0);

  batchRef.current = batch;

  const clearScheduledRefresh = useCallback(() => {
    if (timerRef.current !== null) {
      window.clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }, []);

  const cancelInFlightRequest = useCallback(() => {
    requestIdRef.current += 1;
    const activeRequest = requestContextRef.current;
    if (activeRequest) {
      window.clearTimeout(activeRequest.timeoutId);
      activeRequest.controller.abort();
      requestContextRef.current = null;
    }

    requestInFlightRef.current = false;
  }, []);

  const summary = useMemo(() => (batch ? deriveBatchDetailsSummary(batch) : null), [batch]);

  const scheduleRefresh = useCallback(
    (delayMs: number, refresh: () => void) => {
      clearScheduledRefresh();
      timerRef.current = window.setTimeout(refresh, delayMs);
    },
    [clearScheduledRefresh],
  );

  const refreshRef = useRef<() => void>(() => {});

  const loadBatch = useCallback(
    async (reason: RefreshReason) => {
      const normalizedBatchId = batchId?.trim() ?? '';
      if (!normalizedBatchId) {
        clearScheduledRefresh();
        cancelInFlightRequest();
        setBatch(null);
        setBlockingError({
          title: 'Batch identifier unavailable',
          message: 'A valid batch identifier is required before batch details can be loaded.',
          details: [],
        });
        setRefreshWarning(null);
        setIsInitialLoading(false);
        setIsRefreshing(false);
        setIsAutoRefreshPaused(false);
        return;
      }

      if (requestInFlightRef.current) {
        return;
      }

      clearScheduledRefresh();
      requestInFlightRef.current = true;
      const requestId = requestIdRef.current + 1;
      requestIdRef.current = requestId;
      const controller = new AbortController();
      const timeoutId = window.setTimeout(() => controller.abort(), batchDetailsConfig.requestTimeoutMs);
      requestContextRef.current = { requestId, controller, timeoutId };

      const hasData = batchRef.current !== null;
      if (reason === 'manual' || reason === 'resume') {
        setRefreshWarning(null);
      }

      if (hasData) {
        setIsRefreshing(true);
      } else {
        setIsInitialLoading(true);
      }

      try {
        const result = await fetchBatchDetails(normalizedBatchId, controller.signal);
        if (requestIdRef.current !== requestId) {
          return;
        }

        const retrievedAt = new Date();

        retryAttemptRef.current = 0;
        setBatch(result.data);
        setBlockingError(null);
        setRefreshWarning(null);
        setLastUpdatedAt(retrievedAt);
        setIsAutoRefreshPaused(false);

        if (shouldPollBatchStatus(result.data.status)) {
          scheduleRefresh(batchDetailsConfig.pollIntervalMs, () => {
            void loadBatch('poll');
          });
        }
      } catch (error) {
        if (requestIdRef.current !== requestId) {
          return;
        }

        const willRetry = isTransientApiError(error) && retryAttemptRef.current < batchDetailsConfig.retryDelaysMs.length;
        const nextRetryDelay = willRetry ? batchDetailsConfig.retryDelaysMs[retryAttemptRef.current] : null;
        const exhaustedRetries = isTransientApiError(error) && !willRetry;
        const messageState = toMessageState(error, hasData, exhaustedRetries);

        if (hasData) {
          setRefreshWarning(messageState);
          setIsAutoRefreshPaused(exhaustedRetries);
        } else {
          setBlockingError(messageState);
          setIsAutoRefreshPaused(exhaustedRetries);
        }

        if (willRetry && nextRetryDelay !== null) {
          retryAttemptRef.current += 1;
          scheduleRefresh(nextRetryDelay, () => {
            void loadBatch('poll');
          });
        }
      } finally {
        window.clearTimeout(timeoutId);

        if (requestContextRef.current?.requestId === requestId) {
          requestContextRef.current = null;
          requestInFlightRef.current = false;
          setIsInitialLoading(false);
          setIsRefreshing(false);
        }
      }
    },
    [batchId, cancelInFlightRequest, clearScheduledRefresh, scheduleRefresh],
  );

  const refreshBatch = useCallback(() => {
    retryAttemptRef.current = 0;
    void loadBatch(batchRef.current ? 'manual' : 'initial');
  }, [loadBatch]);

  refreshRef.current = refreshBatch;

  useEffect(() => {
    retryAttemptRef.current = 0;
    batchRef.current = null;
    setBatch(null);
    setBlockingError(null);
    setRefreshWarning(null);
    setIsAutoRefreshPaused(false);
    setLastUpdatedAt(null);
    setIsInitialLoading(true);
    setIsRefreshing(false);

    void loadBatch('initial');

    return () => {
      clearScheduledRefresh();
      cancelInFlightRequest();
    };
  }, [batchId, cancelInFlightRequest, clearScheduledRefresh, loadBatch]);

  useEffect(() => {
    const onFocus = () => {
      void loadBatch('resume');
    };

    const onVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        void loadBatch('resume');
      }
    };

    window.addEventListener('focus', onFocus);
    document.addEventListener('visibilitychange', onVisibilityChange);
    return () => {
      window.removeEventListener('focus', onFocus);
      document.removeEventListener('visibilitychange', onVisibilityChange);
    };
  }, [loadBatch]);

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setNow(Date.now());
    }, 1_000);

    return () => {
      window.clearInterval(intervalId);
    };
  }, []);

  const currentElapsedMs = useMemo(() => {
    if (!batch?.createdUtc) {
      return null;
    }

    const createdAt = new Date(batch.createdUtc).getTime();
    if (Number.isNaN(createdAt)) {
      return null;
    }

    const endTime = batch.completedUtc ? new Date(batch.completedUtc).getTime() : now;
    return endTime >= createdAt ? endTime - createdAt : null;
  }, [batch?.completedUtc, batch?.createdUtc, now]);

  const isLongRunning = Boolean(batch && shouldPollBatchStatus(batch.status) && currentElapsedMs !== null && currentElapsedMs >= batchDetailsConfig.longRunningThresholdMs);

  return {
    batch,
    summary,
    blockingError,
    refreshWarning,
    isInitialLoading,
    isRefreshing,
    isAutoRefreshPaused,
    lastUpdatedAt,
    currentElapsedMs,
    isLongRunning,
    refresh: refreshBatch,
  };
}
