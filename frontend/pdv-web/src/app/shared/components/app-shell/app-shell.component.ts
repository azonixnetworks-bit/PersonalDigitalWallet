import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppTopbarComponent } from '../app-topbar/app-topbar.component';
@Component({selector:'app-shell',standalone:true,imports:[RouterOutlet,AppTopbarComponent],templateUrl:'./app-shell.component.html',styleUrl:'./app-shell.component.css',changeDetection:ChangeDetectionStrategy.OnPush})
export class AppShellComponent {}
