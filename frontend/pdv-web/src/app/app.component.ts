import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastHostComponent } from './shared/components/toast-host/toast-host.component';
@Component({selector:'app-root',standalone:true,imports:[RouterOutlet,ToastHostComponent],template:'<router-outlet /><app-toast-host />',changeDetection:ChangeDetectionStrategy.OnPush})
export class AppComponent {}
