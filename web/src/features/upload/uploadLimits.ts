const defaultUploadLimits = {
  maxRequestBytes: 10 * 1024 * 1024,
  maxFileBytes: 2 * 1024 * 1024,
  maxFileCount: 32,
} as const;

function toPositiveInt(value: string | undefined, fallback: number): number {
  const parsed = Number.parseInt(value ?? '', 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

export const uploadLimits = {
  maxRequestBytes: toPositiveInt(import.meta.env.VITE_UPLOAD_MAX_REQUEST_BYTES, defaultUploadLimits.maxRequestBytes),
  maxFileBytes: toPositiveInt(import.meta.env.VITE_UPLOAD_MAX_FILE_BYTES, defaultUploadLimits.maxFileBytes),
  maxFileCount: toPositiveInt(import.meta.env.VITE_UPLOAD_MAX_FILE_COUNT, defaultUploadLimits.maxFileCount),
} as const;

export type UploadValidationResult = {
  errors: string[];
  totalBytes: number;
};

export function validateSelectedFiles(files: File[]): UploadValidationResult {
  const errors: string[] = [];

  if (files.length === 0) {
    errors.push('Select at least one file before submitting a batch.');
  }

  if (files.length > uploadLimits.maxFileCount) {
    errors.push(`Too many files selected (${files.length}). Maximum allowed is ${uploadLimits.maxFileCount}.`);
  }

  let totalBytes = 0;

  files.forEach((file, index) => {
    totalBytes += file.size;

    if (file.size === 0) {
      errors.push(`File ${index + 1} (${file.name}) is empty.`);
    }

    if (file.size > uploadLimits.maxFileBytes) {
      errors.push(`File ${index + 1} (${file.name}) exceeds ${uploadLimits.maxFileBytes} bytes.`);
    }
  });

  if (totalBytes > uploadLimits.maxRequestBytes) {
    errors.push(`Combined file size exceeds ${uploadLimits.maxRequestBytes} bytes.`);
  }

  return { errors, totalBytes };
}
