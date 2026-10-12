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
    render(<App />);

    expect(screen.getByRole('navigation', { name: 'Main navigation' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Dashboard' })).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('button', { name: 'Go to upload page' })).toBeInTheDocument();
    expect(screen.getByText('Operational overview unavailable')).toBeInTheDocument();
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

    expect(await screen.findByRole('heading', { name: 'Batch details' })).toBeInTheDocument();
    expect(screen.getByText('test-batch-123')).toBeInTheDocument();
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
