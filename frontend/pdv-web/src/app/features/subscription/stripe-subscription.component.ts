import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SubscriptionComponent } from './subscription.component';

@Component({
  selector: 'app-stripe-subscription',
  standalone: true,
  imports: [SubscriptionComponent],
  template: '<app-subscription />',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StripeSubscriptionComponent {}
