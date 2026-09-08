import { Injectable, signal } from '@angular/core';

export type NotificationTone = 'info' | 'success' | 'warning' | 'error';
export interface AppNotification { id: number; message: string; tone: NotificationTone; }

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private sequence = 0;
  readonly notifications = signal<AppNotification[]>([]);

  show(message: string, tone: NotificationTone = 'info', durationMs = 4200): number {
    const id = ++this.sequence;
    this.notifications.update(items => [...items, { id, message, tone }]);
    if (durationMs > 0) window.setTimeout(() => this.dismiss(id), durationMs);
    return id;
  }

  success(message: string): number { return this.show(message, 'success'); }
  error(message: string): number { return this.show(message, 'error', 6000); }
  warning(message: string): number { return this.show(message, 'warning', 5200); }
  dismiss(id: number): void { this.notifications.update(items => items.filter(item => item.id !== id)); }
  clear(): void { this.notifications.set([]); }
}
