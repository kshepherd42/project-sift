import type { ReactNode } from 'react';

type SectionCardProps = {
  title: string;
  children: ReactNode;
  subtitle?: string;
};

export function SectionCard({ title, subtitle, children }: SectionCardProps) {
  return (
    <section className="section-card" aria-label={title}>
      <header>
        <h2>{title}</h2>
        {subtitle && <p>{subtitle}</p>}
      </header>
      <div>{children}</div>
    </section>
  );
}
