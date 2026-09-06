import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PhasePlaceholderComponent } from '../../shared/components/phase-placeholder/phase-placeholder.component';
@Component({selector:'app-adminsubscriptions',standalone:true,imports:[PhasePlaceholderComponent],template:'<app-phase-placeholder title="Admin subscriptions" detail="Admin subscription UI/API conversion comes in the admin phase." />',changeDetection:ChangeDetectionStrategy.OnPush})
export class AdminSubscriptionsComponent {}
