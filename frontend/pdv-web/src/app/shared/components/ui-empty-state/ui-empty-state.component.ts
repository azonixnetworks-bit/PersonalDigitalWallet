import { ChangeDetectionStrategy, Component, input } from '@angular/core';
@Component({selector:'app-ui-empty-state',standalone:true,templateUrl:'./ui-empty-state.component.html',styleUrl:'./ui-empty-state.component.css',changeDetection:ChangeDetectionStrategy.OnPush})
export class UiEmptyStateComponent { readonly title=input.required<string>(); readonly description=input<string>(); readonly icon=input('◇'); }
