export function formatTimestamp(value: Date | string | null): string {
  if (!value) {
    return 'Unavailable';
  }

  const parsed = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return 'Unavailable';
  }

  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(parsed);
}

export function formatCount(value: number | null): string {
  if (value === null || Number.isNaN(value)) {
    return 'Unavailable';
  }

  return new Intl.NumberFormat().format(value);
}

export function formatFileSize(bytes: number | null): string {
  if (bytes === null || bytes < 0 || Number.isNaN(bytes)) {
    return 'Unavailable';
  }

  if (bytes < 1024) {
    return `${bytes} B`;
  }

  const kibibytes = bytes / 1024;
  if (kibibytes < 1024) {
    return `${kibibytes.toFixed(1)} KiB`;
  }

  const mebibytes = kibibytes / 1024;
  return `${mebibytes.toFixed(1)} MiB`;
}

export function formatDuration(milliseconds: number | null): string {
  if (milliseconds === null || milliseconds < 0 || Number.isNaN(milliseconds)) {
    return 'Unavailable';
  }

  const totalSeconds = Math.floor(milliseconds / 1_000);
  if (totalSeconds <= 0) {
    return '<1s';
  }

  const days = Math.floor(totalSeconds / 86_400);
  const hours = Math.floor((totalSeconds % 86_400) / 3_600);
  const minutes = Math.floor((totalSeconds % 3_600) / 60);
  const seconds = totalSeconds % 60;

  const parts: string[] = [];
  if (days > 0) {
    parts.push(`${days}d`);
  }

  if (hours > 0) {
    parts.push(`${hours}h`);
  }

  if (minutes > 0) {
    parts.push(`${minutes}m`);
  }

  if (seconds > 0 && parts.length < 2) {
    parts.push(`${seconds}s`);
  }

  return parts.join(' ');
}
