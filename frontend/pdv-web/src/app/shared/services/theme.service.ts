import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

export type PdvTheme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly storageKey = 'pdv-theme';
  readonly theme = signal<PdvTheme>(this.resolveInitialTheme());

  constructor() {
    this.apply(this.theme());
  }

  toggle(): void {
    const next: PdvTheme = this.theme() === 'dark' ? 'light' : 'dark';
    this.theme.set(next);
    this.apply(next);
  }

  set(theme: PdvTheme): void {
    this.theme.set(theme);
    this.apply(theme);
  }

  private resolveInitialTheme(): PdvTheme {
    try {
      const saved = localStorage.getItem(this.storageKey);
      if (saved === 'light' || saved === 'dark') return saved;
    } catch {
      // Storage is optional. Fall through to system preference.
    }

    return globalThis.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private apply(theme: PdvTheme): void {
    const root = this.document.documentElement;
    root.setAttribute('data-theme', theme);
    root.style.colorScheme = theme;
    try { localStorage.setItem(this.storageKey, theme); } catch { /* no-op */ }
  }
}
