import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import App from '../../App';
import { fetchBatchDetails } from '../../features/batches/gateway/batchDetailsGateway';

vi.mock('../../features/batches/gateway/batchDetailsGateway', () => ({
  fetchBatchDetails: vi.fn(),
}));

describe('App', () => {
  beforeEach(() => {
    window.location.hash = '#/dashboard';
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    vi.restoreAllMocks();
    vi.mocked(fetchBatchDetails).mockResolvedValue({
      statusCode: 200,
      data: {
        batchId: 'test-batch-123',
        status: 'Completed',
        createdUtc: '2026-10-12T12:00:00Z',
        completedUtc: '2026-10-12T12:00:04Z',
        responseWarnings: [],
        payloads: [],
      },
    });
  });

  it('renders dashboard shell and discoverable upload entry point', async () => {
    render(<App />);

    expect(screen.getByRole('navigation', { name: 'Main navigation' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('button', { name: 'Go to upload page' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Quick actions' })).toBeInTheDocument();
    expect(await screen.findByText('Recent activity unavailable')).toBeInTheDocument();
  });

  it('navigates to upload route', async () => {
    render(<App />);

    fireEvent.click(screen.getByRole('link', { name: 'Upload' }));

    expect(await screen.findByRole('heading', { name: 'Upload' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Upload' })).toHaveAttribute('aria-current', 'page');
  });

  it('renders batch details placeholder for hash batch route', async () => {
    window.location.hash = '#/batches/test-batch-123';

    render(<App />);

    expect(await screen.findByText('test-batch-123')).toBeInTheDocument();
    expect(fetchBatchDetails).toHaveBeenCalledWith('test-batch-123', expect.any(AbortSignal));
  });

  it('falls back to dashboard when batch hash contains invalid encoding', async () => {
    window.location.hash = '#/batches/%E0%A4%A';

    render(<App />);

    expect(await screen.findByRole('heading', { name: 'Dashboard' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveAttribute('aria-current', 'page');
  });

  it('toggles dark mode theme and persists choice', async () => {
    render(<App />);

    const toggleButton = screen.getByRole('button', { name: 'Toggle dark mode' });
    fireEvent.click(toggleButton);

    await waitFor(() => {
      expect(document.documentElement.dataset.theme).toBe('dark');
      expect(localStorage.getItem('projectsift-theme')).toBe('dark');
    });
  });

  it('does not call the API while dashboard data is unavailable', () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch');
    render(<App />);

    expect(fetchMock).not.toHaveBeenCalled();
  });
});
