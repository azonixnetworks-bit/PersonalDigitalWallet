import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PhasePlaceholderComponent } from '../../shared/components/phase-placeholder/phase-placeholder.component';
@Component({selector:'app-subscription',standalone:true,imports:[PhasePlaceholderComponent],template:'<app-phase-placeholder title="Subscription" detail="Subscription UI conversion will preserve current backend contracts." />',changeDetection:ChangeDetectionStrategy.OnPush})
export class SubscriptionComponent {}
