import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PhasePlaceholderComponent } from '../../shared/components/phase-placeholder/phase-placeholder.component';
@Component({selector:'app-stripesubscription',standalone:true,imports:[PhasePlaceholderComponent],template:'<app-phase-placeholder title="Stripe subscription" detail="Stripe UI conversion will preserve existing payment behavior." />',changeDetection:ChangeDetectionStrategy.OnPush})
export class StripeSubscriptionComponent {}
