type BadgeTone = 'success' | 'warning' | 'error' | 'info' | 'neutral';

type StatusBadgeProps = {
  status: string;
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
    case 'recovered':
      return 'info';
    case 'failed':
      return 'error';
    default:
      return 'neutral';
  }
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const tone = resolveTone(status);

  return (
    <span className={`status-badge status-${tone}`} aria-label={`Status ${status}`}>
      {status}
    </span>
  );
}
