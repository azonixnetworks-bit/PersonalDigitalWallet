import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AdminTopbarComponent } from '../admin-topbar/admin-topbar.component';
@Component({selector:'app-admin-shell',standalone:true,imports:[RouterOutlet,AdminTopbarComponent],templateUrl:'./admin-shell.component.html',styleUrl:'./admin-shell.component.css',changeDetection:ChangeDetectionStrategy.OnPush})
export class AdminShellComponent {}
