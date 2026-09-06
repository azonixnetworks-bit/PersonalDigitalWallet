import { ChangeDetectionStrategy, Component, input } from '@angular/core';
export type AlertTone = 'info' | 'success' | 'warning' | 'error';
@Component({selector:'app-ui-alert',standalone:true,templateUrl:'./ui-alert.component.html',styleUrl:'./ui-alert.component.css',changeDetection:ChangeDetectionStrategy.OnPush})
export class UiAlertComponent { readonly message=input.required<string>(); readonly tone=input<AlertTone>('info'); }
