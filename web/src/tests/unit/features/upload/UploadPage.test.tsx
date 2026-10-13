import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../../gateway/apiClient';
import { submitBatch } from '../../../../features/upload/gateway/batchGateway';
import { UploadPage } from '../../../../features/upload/UploadPage';

vi.mock('../../../../features/upload/gateway/batchGateway', () => ({
  submitBatch: vi.fn(),
}));

describe('UploadPage', () => {
  const onBatchCreated = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('adds files from picker, keeps duplicate filenames, and allows removal', async () => {
    render(<UploadPage onBatchCreated={onBatchCreated} />);

    const picker = screen.getByLabelText('Choose files') as HTMLInputElement;
    const first = new File(['abc'], 'same.bin', { type: 'application/octet-stream' });
    const second = new File(['abcd'], 'same.bin', { type: 'application/octet-stream' });

    fireEvent.change(picker, { target: { files: [first, second] } });

    expect(await screen.findAllByText('same.bin')).toHaveLength(2);

    fireEvent.click(screen.getAllByRole('button', { name: 'Remove' })[0]);

    expect(screen.getAllByText('same.bin')).toHaveLength(1);
  });

  it('adds dropped files', async () => {
    render(<UploadPage onBatchCreated={onBatchCreated} />);

    const dropTarget = screen.getByText('Drop files here or choose from your device.').closest('div');
    expect(dropTarget).not.toBeNull();

    const dropped = new File(['{}'], 'dropped.bin');
    fireEvent.drop(dropTarget!, {
      dataTransfer: {
        files: [dropped],
      },
    });

    expect(await screen.findByText('dropped.bin')).toBeInTheDocument();
  });

  it('prevents submission when client-side validation fails', async () => {
    render(<UploadPage onBatchCreated={onBatchCreated} />);

    const picker = screen.getByLabelText('Choose files') as HTMLInputElement;
    const oversized = new File([new Uint8Array(2_097_153)], 'too-large.bin');

    fireEvent.change(picker, { target: { files: [oversized] } });

    expect(await screen.findByText(/exceeds 2097152 bytes/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Submit batch' })).toBeDisabled();
  });

  it('highlights file cards that have file-level validation issues', async () => {
    render(<UploadPage onBatchCreated={onBatchCreated} />);

    const picker = screen.getByLabelText('Choose files') as HTMLInputElement;
    const empty = new File([], 'empty.bin');
    const valid = new File(['{}'], 'valid.bin');

    fireEvent.change(picker, { target: { files: [empty, valid] } });

    const emptyLabel = await screen.findByText('empty.bin');
    const emptyCard = emptyLabel.closest('li');

    expect(emptyCard).toHaveClass('upload-file-invalid');
    expect(screen.getByText('Empty file')).toBeInTheDocument();
    expect(screen.getByText('valid.bin').closest('li')).not.toHaveClass('upload-file-invalid');
  });

  it('submits files and navigates after successful batch creation', async () => {
    vi.mocked(submitBatch).mockResolvedValue({
      statusCode: 202,
      data: {
        batchId: 'batch-123',
        resourceUrl: '/v1/batches/batch-123',
      },
    });

    render(<UploadPage onBatchCreated={onBatchCreated} />);

    const picker = screen.getByLabelText('Choose files') as HTMLInputElement;
    const file = new File(['{}'], 'payload.bin');
    fireEvent.change(picker, { target: { files: [file] } });

    fireEvent.click(screen.getByRole('button', { name: 'Submit batch' }));

    await waitFor(() => {
      expect(submitBatch).toHaveBeenCalledTimes(1);
      expect(onBatchCreated).toHaveBeenCalledWith('batch-123');
    });
  });

  it('prevents duplicate submission while request is in progress', async () => {
    type SubmissionResult = { statusCode: number; data: { batchId: string; resourceUrl: string } };
    let resolver: ((value: SubmissionResult) => void) | undefined;

    vi.mocked(submitBatch).mockImplementation(
      () =>
        new Promise<SubmissionResult>((resolve) => {
          resolver = resolve;
        }),
    );

    render(<UploadPage onBatchCreated={onBatchCreated} />);

    const picker = screen.getByLabelText('Choose files') as HTMLInputElement;
    fireEvent.change(picker, { target: { files: [new File(['{}'], 'payload.bin')] } });

    const submitButton = screen.getByRole('button', { name: 'Submit batch' });
    submitButton.focus();
    fireEvent.click(submitButton);
    fireEvent.click(submitButton);

    expect(submitBatch).toHaveBeenCalledTimes(1);

    if (resolver) {
      resolver({ statusCode: 202, data: { batchId: 'batch-abc', resourceUrl: '/v1/batches/batch-abc' } });
    }

    await waitFor(() => {
      expect(onBatchCreated).toHaveBeenCalledWith('batch-abc');
    });
  });

  it('shows acknowledgment dialog with backend validation details on failure', async () => {
    vi.mocked(submitBatch).mockRejectedValue(
      new ApiError('Upload validation failed.', 400, {
        code: 'INVALID_UPLOAD',
        details: ['File 1 exceeds allowed size.', 'File 2 is empty.'],
      }),
    );

    render(<UploadPage onBatchCreated={onBatchCreated} />);

    const picker = screen.getByLabelText('Choose files') as HTMLInputElement;
    fireEvent.change(picker, { target: { files: [new File(['{}'], 'payload.bin')] } });

    const submitButton = screen.getByRole('button', { name: 'Submit batch' });
    submitButton.focus();
    fireEvent.click(submitButton);

    const dialog = await screen.findByRole('dialog', { name: 'Upload failed' });
    expect(dialog).toBeInTheDocument();
    expect(dialog.querySelector('button')).toHaveFocus();
    expect(screen.getByText('Upload rejected by backend validation')).toBeInTheDocument();

    fireEvent.click(screen.getByText('Technical details'));
    expect(screen.getByText('File 1 exceeds allowed size.')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Acknowledge upload error' }));

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Upload failed' })).not.toBeInTheDocument();
    });
    expect(submitButton).toHaveFocus();

    expect(screen.getByText('payload.bin')).toBeInTheDocument();
  });

  it('dismisses the upload error dialog with Escape', async () => {
    vi.mocked(submitBatch).mockRejectedValue(new Error('Network unavailable.'));

    render(<UploadPage onBatchCreated={onBatchCreated} />);

    fireEvent.change(screen.getByLabelText('Choose files'), { target: { files: [new File(['{}'], 'payload.bin')] } });
    fireEvent.click(screen.getByRole('button', { name: 'Submit batch' }));

    const dialog = await screen.findByRole('dialog', { name: 'Upload failed' });
    fireEvent.keyDown(document, { key: 'Escape' });

    await waitFor(() => {
      expect(screen.queryByRole('dialog', { name: 'Upload failed' })).not.toBeInTheDocument();
    });
    expect(dialog).not.toBeInTheDocument();
  });
});
