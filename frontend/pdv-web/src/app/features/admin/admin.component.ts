import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PhasePlaceholderComponent } from '../../shared/components/phase-placeholder/phase-placeholder.component';
@Component({selector:'app-admin',standalone:true,imports:[PhasePlaceholderComponent],template:'<app-phase-placeholder title="Admin dashboard" detail="Admin route is protected by the Angular admin guard; UI migration comes later." />',changeDetection:ChangeDetectionStrategy.OnPush})
export class AdminComponent {}
