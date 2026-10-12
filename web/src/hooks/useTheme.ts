import { useEffect, useState } from 'react';

export type ThemeMode = 'light' | 'dark';

const storageKey = 'projectsift-theme';
const isBrowser = typeof window !== 'undefined';

function isThemeMode(value: string | null): value is ThemeMode {
  return value === 'light' || value === 'dark';
}

function getInitialTheme(): ThemeMode {
  if (!isBrowser) {
    return 'light';
  }

  try {
    const stored = window.localStorage.getItem(storageKey);
    return isThemeMode(stored) ? stored : 'light';
  } catch {
    return 'light';
  }
}

export function useTheme() {
  const [theme, setTheme] = useState<ThemeMode>(getInitialTheme);

  useEffect(() => {
    if (!isBrowser) {
      return;
    }

    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;

    try {
      window.localStorage.setItem(storageKey, theme);
    } catch {
      // Ignore storage write failures (private mode / restricted storage).
    }
  }, [theme]);

  const toggleTheme = () => {
    setTheme((current) => (current === 'light' ? 'dark' : 'light'));
  };

  return { theme, toggleTheme };
}
