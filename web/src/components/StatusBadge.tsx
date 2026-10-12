type BadgeTone = 'success' | 'warning' | 'error' | 'info' | 'neutral';

type StatusBadgeProps = {
  status: string;
  tone?: BadgeTone;
  ariaLabel?: string;
};

function normalize(status: string): string {
  return status.trim().toLowerCase();
}

function resolveTone(status: string): BadgeTone {
  switch (normalize(status)) {
    case 'completed':
    case 'successful':
      return 'success';
    case 'processing':
      return 'info';
    case 'recovered':
      return 'warning';
    case 'failed':
      return 'error';
    default:
      return 'neutral';
  }
}

export function StatusBadge({ status, tone, ariaLabel }: StatusBadgeProps) {
  const resolvedTone = tone ?? resolveTone(status);

  return (
    <span className={`status-badge status-${resolvedTone}`} aria-label={ariaLabel ?? `Status ${status}`}>
      {status}
    </span>
  );
}
