import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-brand',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './app-brand.component.html',
  styleUrl: './app-brand.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppBrandComponent {
  readonly admin = input(false);
}
