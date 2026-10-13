import { act, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../../gateway/apiClient';
import { BatchDetailsPage } from '../../../../features/batches/BatchDetailsPage';
import { fetchBatchDetails } from '../../../../features/batches/gateway/batchDetailsGateway';
import type { BatchDetails } from '../../../../features/batches/batchDetailsModel';

vi.mock('../../../../features/batches/gateway/batchDetailsGateway', () => ({
  fetchBatchDetails: vi.fn(),
}));

function createBatch(overrides?: Partial<BatchDetails>): BatchDetails {
  return {
    batchId: 'batch-123',
    status: 'Completed',
    createdUtc: '2026-10-12T12:00:00Z',
    completedUtc: '2026-10-12T12:00:09Z',
    responseWarnings: [],
    payloads: [
      {
        payloadIndex: 0,
        payloadName: null,
        key: 'payload-0',
        diagnostics: [],
        documents: [
          {
            documentIndex: 0,
            documentName: null,
            type: 'Object',
            status: 'Recovered',
            propertyPaths: ['customer.id'],
            diagnostics: [],
            key: 'payload-0-document-0',
          },
        ],
      },
    ],
    ...overrides,
  };
}

async function flushMicrotasks() {
  await act(async () => {
    await Promise.resolve();
  });
}

function createAbortError() {
  return new DOMException('The operation was aborted.', 'AbortError');
}

describe('BatchDetailsPage', () => {
  beforeEach(() => {
    vi.useRealTimers();
    vi.resetAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders summary data and keeps payload groups collapsed initially', async () => {
    vi.mocked(fetchBatchDetails).mockResolvedValue({ statusCode: 200, data: createBatch() });

    const { container } = render(<BatchDetailsPage batchId="batch-123" onNavigateToUpload={vi.fn()} />);

    expect(await screen.findByText('batch-123')).toBeInTheDocument();
    expect(screen.getByText('Name unavailable')).toBeInTheDocument();
    expect(screen.getAllByText('Recovered').length).toBeGreaterThan(0);
    expect(screen.getByText('Successful or recovered')).toBeInTheDocument();

    const payloadGroups = container.querySelectorAll('.result-group-payload');
    expect(payloadGroups).toHaveLength(1);
    expect(payloadGroups[0]).not.toHaveAttribute('open');
  });

  it('copies an individual property path and reports clipboard failure', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    vi.stubGlobal('navigator', { clipboard: { writeText } });
    vi.mocked(fetchBatchDetails).mockResolvedValue({ statusCode: 200, data: createBatch() });

    const { container } = render(<BatchDetailsPage batchId="batch-123" onNavigateToUpload={vi.fn()} />);
    expect(await screen.findByText('batch-123')).toBeInTheDocument();
    const documentGroup = container.querySelector('.result-group-document');
    expect(documentGroup).not.toBeNull();
    fireEvent.click(documentGroup!.querySelector('summary')!);
    fireEvent.click(screen.getByRole('button', { name: 'Copy property path customer.id' }));

    expect(writeText).toHaveBeenCalledWith('customer.id');
    expect(await screen.findByText('Copied customer.id to the clipboard.')).toBeInTheDocument();

    writeText.mockRejectedValueOnce(new Error('Clipboard denied.'));
    fireEvent.click(screen.getByRole('button', { name: 'Copy property path customer.id' }));
    expect(await screen.findByText('Clipboard access is unavailable in this browser context.')).toBeInTheDocument();
  });

  it('polls while the batch is processing and stops after completion', async () => {
    vi.useFakeTimers();
    vi.mocked(fetchBatchDetails)
      .mockResolvedValueOnce({ statusCode: 200, data: createBatch({ status: 'Processing', completedUtc: null }) })
      .mockResolvedValueOnce({ statusCode: 200, data: createBatch({ status: 'Completed' }) });

    render(<BatchDetailsPage batchId="batch-123" onNavigateToUpload={vi.fn()} />);

    await flushMicrotasks();

    expect(fetchBatchDetails).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(3_000);
    });

    await flushMicrotasks();

    expect(fetchBatchDetails).toHaveBeenCalledTimes(2);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(10_000);
    });
    expect(fetchBatchDetails).toHaveBeenCalledTimes(2);
  });

  it('preserves last successful data and allows manual refresh after transient polling failure', async () => {
    vi.useFakeTimers();
    vi.mocked(fetchBatchDetails)
      .mockResolvedValueOnce({ statusCode: 200, data: createBatch({ status: 'Processing', completedUtc: null }) })
      .mockRejectedValueOnce(new ApiError('Temporary outage.', 503, { code: 'QUEUE_UNAVAILABLE' }))
      .mockResolvedValueOnce({ statusCode: 200, data: createBatch({ status: 'Completed' }) });

    render(<BatchDetailsPage batchId="batch-123" onNavigateToUpload={vi.fn()} />);

    await flushMicrotasks();

    expect(screen.getByText('batch-123')).toBeInTheDocument();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(3_000);
    });

    await flushMicrotasks();

    expect(screen.getByText('Showing last successful retrieval')).toBeInTheDocument();
    expect(screen.getByText('batch-123')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Refresh batch details' }));

    await flushMicrotasks();

    expect(fetchBatchDetails).toHaveBeenCalledTimes(3);
    expect(screen.queryByText('Showing last successful retrieval')).not.toBeInTheDocument();
  });

  it('shows a not-found state for missing batches', async () => {
    vi.mocked(fetchBatchDetails).mockRejectedValue(new ApiError('Batch was not found.', 404, { code: 'BATCH_NOT_FOUND' }));

    render(<BatchDetailsPage batchId="missing-batch" onNavigateToUpload={vi.fn()} />);

    expect(await screen.findByText('Batch not found')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument();
  });

  it('reports a timed-out initial request and schedules a bounded retry', async () => {
    vi.useFakeTimers();
    vi.mocked(fetchBatchDetails)
      .mockImplementationOnce(
        (_batchId, signal) =>
          new Promise((_, reject) => {
            signal?.addEventListener('abort', () => reject(createAbortError()), { once: true });
          }),
      )
      .mockResolvedValueOnce({ statusCode: 200, data: createBatch() });

    render(<BatchDetailsPage batchId="batch-123" onNavigateToUpload={vi.fn()} />);

    await flushMicrotasks();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(10_000);
    });

    expect(screen.getByText('Batch details timed out')).toBeInTheDocument();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(2_000);
    });
    await flushMicrotasks();

    expect(screen.getByText('batch-123')).toBeInTheDocument();
    expect(fetchBatchDetails).toHaveBeenCalledTimes(2);
  });

  it('shows long-running processing notice without inventing failure', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-10-12T12:00:20Z'));
    vi.mocked(fetchBatchDetails).mockResolvedValue({
      statusCode: 200,
      data: createBatch({ status: 'Processing', completedUtc: null, createdUtc: '2026-10-12T12:00:00Z' }),
    });

    render(<BatchDetailsPage batchId="batch-123" onNavigateToUpload={vi.fn()} />);

    await flushMicrotasks();

    expect(screen.getByText('Taking longer than expected')).toBeInTheDocument();
    expect(screen.getByText(/still processing/i)).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('Batch processing is taking longer than expected.');
  });

  it('ignores aborted stale requests after batch navigation changes', async () => {
    vi.useFakeTimers();

    vi.mocked(fetchBatchDetails)
      .mockImplementationOnce(
        (_batchId, signal) =>
          new Promise((_, reject) => {
            signal?.addEventListener('abort', () => reject(createAbortError()), { once: true });
          }),
      )
      .mockResolvedValueOnce({
        statusCode: 200,
        data: createBatch({ batchId: 'batch-456', status: 'Completed' }),
      });

    const { rerender } = render(<BatchDetailsPage batchId="batch-123" onNavigateToUpload={vi.fn()} />);

    await flushMicrotasks();

    rerender(<BatchDetailsPage batchId="batch-456" onNavigateToUpload={vi.fn()} />);

    await flushMicrotasks();

    expect(screen.getByText('batch-456')).toBeInTheDocument();
    expect(screen.queryByText('Unable to load batch details')).not.toBeInTheDocument();
    expect(screen.queryByText('Showing last successful retrieval')).not.toBeInTheDocument();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(10_000);
    });

    expect(fetchBatchDetails).toHaveBeenCalledTimes(2);
  });

  it('shows a blocking error when navigation to a new batch fails', async () => {
    vi.mocked(fetchBatchDetails)
      .mockResolvedValueOnce({ statusCode: 200, data: createBatch() })
      .mockRejectedValueOnce(new ApiError('Batch was not found.', 404, { code: 'BATCH_NOT_FOUND' }));

    const { rerender } = render(<BatchDetailsPage batchId="batch-123" onNavigateToUpload={vi.fn()} />);

    expect(await screen.findByText('batch-123')).toBeInTheDocument();

    rerender(<BatchDetailsPage batchId="missing-batch" onNavigateToUpload={vi.fn()} />);

    expect(await screen.findByText('Batch not found')).toBeInTheDocument();
    expect(screen.queryByText('batch-123')).not.toBeInTheDocument();
  });
});
