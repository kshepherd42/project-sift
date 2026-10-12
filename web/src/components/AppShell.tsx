import type { ReactNode } from 'react';
import { useTheme } from '../hooks/useTheme';
import type { AppRoute, NavigableRoute } from '../features/routes/useHashRoute';
import { Button } from './Button';

type AppShellProps = {
  currentRoute: AppRoute;
  onNavigate: (route: NavigableRoute) => void;
  children: ReactNode;
};

function NavLink({
  href,
  label,
  isActive,
  onClick,
}: {
  href: string;
  label: string;
  isActive: boolean;
  onClick: () => void;
}) {
  return (
    <a
      className={`nav-link${isActive ? ' active' : ''}`}
      href={href}
      aria-current={isActive ? 'page' : undefined}
      onClick={(event) => {
        event.preventDefault();
        onClick();
      }}
    >
      {label}
    </a>
  );
}

export function AppShell({ currentRoute, onNavigate, children }: AppShellProps) {
  const { theme, toggleTheme } = useTheme();
  const currentSectionLabel =
    currentRoute === 'dashboard' ? 'Dashboard' : currentRoute === 'upload' ? 'Upload' : 'Batch details';

  return (
    <div className="app-shell">
      <aside className="sidebar" aria-label="Main navigation">
        <div className="brand">ProjectSift</div>
        <nav aria-label="Main navigation">
          <NavLink
            href="#/dashboard"
            label="Dashboard"
            isActive={currentRoute === 'dashboard'}
            onClick={() => onNavigate('dashboard')}
          />
          <NavLink
            href="#/upload"
            label="Upload"
            isActive={currentRoute === 'upload'}
            onClick={() => onNavigate('upload')}
          />
        </nav>
      </aside>

      <div className="content-shell">
        <header className="topbar">
          <div>
            <p className="topbar-label">Batch processing workflow</p>
            <strong>{currentSectionLabel}</strong>
          </div>
          <Button variant="secondary" onClick={toggleTheme} aria-label="Toggle dark mode">
            {theme === 'light' ? 'Dark mode' : 'Light mode'}
          </Button>
        </header>

        <main className="main-content">{children}</main>
      </div>
    </div>
  );
}
