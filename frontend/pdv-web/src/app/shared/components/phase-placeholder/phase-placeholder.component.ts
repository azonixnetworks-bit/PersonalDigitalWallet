import { ChangeDetectionStrategy, Component, input } from '@angular/core';
@Component({
  selector: 'app-phase-placeholder', standalone: true,
  template: '<div class="container"><section class="phase-card"><span>Angular migration</span><h1>{{ title() }}</h1><p>{{ detail() }}</p><strong>Foundation ready — feature conversion is scheduled for the next migration phase.</strong></section></div>',
  styles: ['.phase-card{max-width:780px;padding:32px;border:1px solid rgba(255,255,255,.1);border-radius:18px;background:rgba(255,255,255,.04)}span{font-size:.78rem;text-transform:uppercase;letter-spacing:.16em;color:#88aef8}h1{margin:.45rem 0 .7rem;font-size:clamp(2rem,5vw,3.2rem)}p{color:#b9c7db;line-height:1.7}strong{display:block;margin-top:20px;color:#dfeaff}'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PhasePlaceholderComponent {
  readonly title = input.required<string>();
  readonly detail = input('Existing HTML/CSS/JS behavior will be migrated without changing backend API contracts.');
}
