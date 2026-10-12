import type { ReactNode } from 'react';

type DataStateKind = 'loading' | 'error' | 'empty' | 'unavailable';

type DataStateProps = {
  kind: DataStateKind;
  title: string;
  description: string;
  action?: ReactNode;
};

export function DataState({ kind, title, description, action }: DataStateProps) {
  const role = kind === 'error' ? 'alert' : kind === 'loading' ? 'status' : undefined;

  return (
    <div className={`data-state data-state-${kind}`} role={role}>
      <h3>{title}</h3>
      <p>{description}</p>
      {action}
    </div>
  );
}
