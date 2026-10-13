type RecentBatchActivityEntry = {
  batchId: string;
  lastViewedUtc: string;
};

const recentBatchActivityStorageKey = 'projectsift-recent-batches';
const maxRecentEntries = 8;

function isRecentBatchActivityEntry(value: unknown): value is RecentBatchActivityEntry {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const candidate = value as Partial<RecentBatchActivityEntry>;
  return typeof candidate.batchId === 'string' && typeof candidate.lastViewedUtc === 'string';
}

function parseEntries(raw: string | null): RecentBatchActivityEntry[] {
  if (!raw) {
    return [];
  }

  try {
    const parsed = JSON.parse(raw);
    if (!Array.isArray(parsed)) {
      return [];
    }

    return parsed
      .filter(isRecentBatchActivityEntry)
      .map((entry) => ({
        batchId: entry.batchId.trim(),
        lastViewedUtc: entry.lastViewedUtc,
      }))
      .filter((entry) => entry.batchId.length > 0)
      .sort((a, b) => Date.parse(b.lastViewedUtc) - Date.parse(a.lastViewedUtc));
  } catch {
    return [];
  }
}

function saveEntries(entries: RecentBatchActivityEntry[]): void {
  try {
    localStorage.setItem(recentBatchActivityStorageKey, JSON.stringify(entries));
  } catch {
    // Ignore storage failures.
  }
}

export function loadRecentBatchActivity(): RecentBatchActivityEntry[] {
  if (typeof window === 'undefined') {
    return [];
  }

  return parseEntries(localStorage.getItem(recentBatchActivityStorageKey));
}

export function recordRecentBatchActivity(batchId: string): void {
  if (typeof window === 'undefined') {
    return;
  }

  const normalizedBatchId = batchId.trim();
  if (!normalizedBatchId) {
    return;
  }

  const remainingEntries = loadRecentBatchActivity().filter((entry) => entry.batchId !== normalizedBatchId);
  const nextEntries = [{ batchId: normalizedBatchId, lastViewedUtc: new Date().toISOString() }, ...remainingEntries]
    .slice(0, maxRecentEntries);

  saveEntries(nextEntries);
}
