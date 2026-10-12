import { useMemo, useRef, useState, type ChangeEventHandler, type DragEventHandler } from 'react';
import { Button } from '../../components/Button';
import { DataState } from '../../components/DataState';
import { PageHeader } from '../../components/PageHeader';
import { SectionCard } from '../../components/SectionCard';
import { ApiError } from '../../gateway/apiClient';
import { submitBatch } from './gateway/batchGateway';
import { formatFileSize } from '../../utils/formatting';
import { uploadLimits, validateSelectedFiles } from './uploadLimits';

type UploadFileEntry = {
  id: number;
  file: File;
};

type SubmissionErrorState = {
  title: string;
  message: string;
  details: string[];
};

type UploadPageProps = {
  onBatchCreated: (batchId: string) => void;
};

function buildSignature(file: File): string {
  return `${file.name}::${file.size}::${file.lastModified}`;
}

export function UploadPage({ onBatchCreated }: UploadPageProps) {
  const [files, setFiles] = useState<UploadFileEntry[]>([]);
  const [selectionNote, setSelectionNote] = useState<string | null>(null);
  const [isDragActive, setIsDragActive] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submissionError, setSubmissionError] = useState<SubmissionErrorState | null>(null);
  const nextIdRef = useRef(1);

  const selectedFiles = useMemo(() => files.map((entry) => entry.file), [files]);
  const validation = useMemo(() => validateSelectedFiles(selectedFiles), [selectedFiles]);
  const canSubmit = files.length > 0 && validation.errors.length === 0 && !isSubmitting;

  const addFiles = (incomingFiles: File[]) => {
    if (incomingFiles.length === 0) {
      setSelectionNote('No files were added. Choose one or more files to continue.');
      return;
    }

    setFiles((current) => {
      const seenSignatures = new Set(current.map((entry) => buildSignature(entry.file)));
      const appended: UploadFileEntry[] = [];
      let duplicateCount = 0;

      for (const file of incomingFiles) {
        const signature = buildSignature(file);
        if (seenSignatures.has(signature)) {
          duplicateCount += 1;
          continue;
        }

        seenSignatures.add(signature);
        appended.push({
          id: nextIdRef.current++,
          file,
        });
      }

      if (duplicateCount > 0 && appended.length === 0) {
        setSelectionNote('That selection was already added.');
      } else if (duplicateCount > 0) {
        setSelectionNote(`${duplicateCount} duplicate file selection event(s) were ignored.`);
      } else {
        setSelectionNote(null);
      }

      return [...current, ...appended];
    });
  };

  const removeFile = (id: number) => {
    setFiles((current) => current.filter((entry) => entry.id !== id));
  };

  const clearFiles = () => {
    setFiles([]);
    setSelectionNote(null);
  };

  const onFilePickerChange: ChangeEventHandler<HTMLInputElement> = (event) => {
    const incoming = Array.from(event.currentTarget.files ?? []);
    addFiles(incoming);
    event.currentTarget.value = '';
  };

  const onDrop: DragEventHandler<HTMLDivElement> = (event) => {
    event.preventDefault();
    setIsDragActive(false);

    const droppedFiles = Array.from(event.dataTransfer.files ?? []);
    if (droppedFiles.length === 0) {
      setSelectionNote('Drop contained no files.');
      return;
    }

    addFiles(droppedFiles);
  };

  const submit = async () => {
    if (!canSubmit) {
      return;
    }

    setIsSubmitting(true);
    setSubmissionError(null);

    const abortController = new AbortController();
    const timeout = setTimeout(() => abortController.abort(), 30_000);

    try {
      const result = await submitBatch(selectedFiles, abortController.signal);
      onBatchCreated(result.data.batchId);
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.statusCode === 400) {
          setSubmissionError({
            title: 'Upload rejected by backend validation',
            message: error.message,
            details: error.details ?? [],
          });
        } else if (error.statusCode === 503) {
          setSubmissionError({
            title: 'Upload queue is currently full',
            message: 'The backend could not accept this batch right now. Acknowledge and retry shortly.',
            details: [error.message, error.code].filter((value): value is string => !!value),
          });
        } else {
          setSubmissionError({
            title: 'Upload failed',
            message: error.message,
            details: [error.code, error.batchId].filter((value): value is string => !!value),
          });
        }
      } else if (error instanceof DOMException && error.name === 'AbortError') {
        setSubmissionError({
          title: 'Upload timed out',
          message: 'The request exceeded the allowed wait time. Check connectivity and retry.',
          details: ['Request aborted after 30 seconds.'],
        });
      } else {
        setSubmissionError({
          title: 'Upload failed',
          message: 'Unable to submit files due to a connectivity or unexpected error.',
          details: [error instanceof Error ? error.message : 'Unknown error'],
        });
      }
    } finally {
      clearTimeout(timeout);
      setIsSubmitting(false);
    }
  };

  return (
    <div className="stack-lg">
      <PageHeader title="Upload" subtitle="Submit one or more files as a single batch using the verified multipart contract." />

      <SectionCard title="Select files" subtitle="Use file picker or drag-and-drop.">
        <div
          className={`drop-zone${isDragActive ? ' active' : ''}`}
          onDragEnter={(event) => {
            event.preventDefault();
            setIsDragActive(true);
          }}
          onDragOver={(event) => {
            event.preventDefault();
            setIsDragActive(true);
          }}
          onDragLeave={(event) => {
            event.preventDefault();
            setIsDragActive(false);
          }}
          onDrop={onDrop}
        >
          <p>Drop files here or choose files manually.</p>
          <label className="btn btn-secondary upload-picker-button">
            Choose files
            <input aria-label="Choose files" className="upload-input" type="file" multiple onChange={onFilePickerChange} />
          </label>
        </div>

        {selectionNote && <p className="selection-note">{selectionNote}</p>}

        <div className="upload-actions">
          <Button variant="secondary" onClick={clearFiles} disabled={files.length === 0 || isSubmitting}>
            Clear selection
          </Button>
        </div>
      </SectionCard>

      <SectionCard title="Selected files" subtitle="Filenames are preserved exactly as selected.">
        {files.length === 0 ? (
          <DataState
            kind="empty"
            title="No files selected"
            description="Choose at least one file to begin validation and submission."
          />
        ) : (
          <>
            <ul className="upload-file-list" aria-label="Selected files">
              {files.map((entry, index) => (
                <li key={entry.id}>
                  <div>
                    <strong>{entry.file.name}</strong>
                    <div className="upload-file-meta">File {index + 1} · {formatFileSize(entry.file.size)}</div>
                  </div>
                  <Button variant="secondary" onClick={() => removeFile(entry.id)} disabled={isSubmitting}>
                    Remove
                  </Button>
                </li>
              ))}
            </ul>
            <p className="upload-total">Total size: {formatFileSize(validation.totalBytes)}</p>
          </>
        )}
      </SectionCard>

      <SectionCard title="Validation" subtitle="Limits are based on current backend upload settings.">
        {validation.errors.length === 0 ? (
          <DataState kind="loading" title="Ready to submit" description="All selected files satisfy current validation rules." />
        ) : (
          <div className="validation-errors" role="alert">
            <h3>Fix these issues before submitting</h3>
            <ul>
              {validation.errors.map((error) => (
                <li key={error}>{error}</li>
              ))}
            </ul>
          </div>
        )}
        <ul className="plain-list upload-limits">
          <li>Max files per batch: {uploadLimits.maxFileCount}</li>
          <li>Max bytes per file: {formatFileSize(uploadLimits.maxFileBytes)}</li>
          <li>Max combined request size: {formatFileSize(uploadLimits.maxRequestBytes)}</li>
        </ul>
      </SectionCard>

      <SectionCard title="Submit" subtitle="Submission sends all selected files in a single multipart request field named files.">
        <div className="upload-actions">
          <Button onClick={() => void submit()} disabled={!canSubmit} aria-label="Submit batch">
            {isSubmitting ? 'Submitting…' : 'Submit batch'}
          </Button>
        </div>
        {isSubmitting && (
          <p className="upload-progress" role="status">
            Upload request in progress.
          </p>
        )}
      </SectionCard>

      {submissionError && (
        <div className="dialog-backdrop" role="presentation">
          <section className="dialog" role="dialog" aria-modal="true" aria-label="Upload failed">
            <h2>{submissionError.title}</h2>
            <p>{submissionError.message}</p>
            {submissionError.details.length > 0 && (
              <details>
                <summary>Technical details</summary>
                <ul className="plain-list">
                  {submissionError.details.map((detail) => (
                    <li key={detail}>{detail}</li>
                  ))}
                </ul>
              </details>
            )}
            <div className="upload-actions">
              <Button onClick={() => setSubmissionError(null)} aria-label="Acknowledge upload error">
                Acknowledge
              </Button>
            </div>
          </section>
        </div>
      )}
    </div>
  );
}
