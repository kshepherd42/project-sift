import type { ReactNode } from 'react';

type StatusPanelProps = {
  title: string;
  children: ReactNode;
};

export function StatusPanel({ title, children }: StatusPanelProps) {
  return (
    <section style={{ border: '1px solid #ddd', borderRadius: 8, padding: 16, maxWidth: 640 }}>
      <h2 style={{ marginTop: 0 }}>{title}</h2>
      {children}
    </section>
  );
}
