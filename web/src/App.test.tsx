import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import App from './App';

describe('App', () => {
  beforeEach(() => {
    window.location.hash = '#/dashboard';
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    vi.restoreAllMocks();
  });

  it('renders dashboard shell and discoverable upload entry point', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ status: 'Healthy' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    render(<App />);

    expect(screen.getByRole('navigation', { name: 'Main navigation' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('button', { name: 'Go to upload page' })).toBeInTheDocument();
    expect(await screen.findByText('Recent activity unavailable')).toBeInTheDocument();
  });

  it('navigates to upload placeholder route', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ status: 'Healthy' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    render(<App />);

    fireEvent.click(screen.getByRole('link', { name: 'Upload' }));

    expect(await screen.findByRole('heading', { name: 'Upload' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Upload' })).toHaveAttribute('aria-current', 'page');
  });

  it('toggles dark mode theme and persists choice', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ status: 'Healthy' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    render(<App />);

    const toggleButton = screen.getByRole('button', { name: 'Toggle dark mode' });
    fireEvent.click(toggleButton);

    await waitFor(() => {
      expect(document.documentElement.dataset.theme).toBe('dark');
      expect(localStorage.getItem('projectsift-theme')).toBe('dark');
    });
  });

  it('shows live health status when dashboard request succeeds', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ status: 'Healthy' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    render(<App />);

    expect(screen.getByRole('status')).toHaveTextContent('Loading dashboard');
    expect(await screen.findByLabelText('Status Healthy')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('shows dashboard error state and retries', async () => {
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

    expect(await screen.findByRole('alert')).toHaveTextContent('Dashboard unavailable');

    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(2);
    });

    expect(await screen.findByLabelText('Status Healthy')).toBeInTheDocument();
  });
});
