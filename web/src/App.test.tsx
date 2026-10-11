import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';

describe('App', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('shows returned HTTP status code and JSON body when request succeeds', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ status: 'Healthy' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    render(<App />);

    expect(screen.getByRole('status')).toHaveTextContent('Loading API health...');

    expect(await screen.findByText('HTTP Status: 200')).toBeInTheDocument();
    expect(screen.getByLabelText('health-json')).toHaveTextContent('"status": "Healthy"');
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('shows a useful error state when the request fails', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Network error'));

    render(<App />);

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Unable to reach the API. Check that the backend is running and try again.',
    );
  });

  it('retries the request when retry is clicked', async () => {
    const fetchMock = vi
      .spyOn(globalThis, 'fetch')
      .mockRejectedValueOnce(new TypeError('Network error'))
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ status: 'Healthy' }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      );

    render(<App />);

    const retryButton = await screen.findByRole('button', { name: 'Retry' });
    fireEvent.click(retryButton);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(2);
    });

    expect(await screen.findByText('HTTP Status: 200')).toBeInTheDocument();
  });
});
