import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PhasePlaceholderComponent } from '../../shared/components/phase-placeholder/phase-placeholder.component';
@Component({selector:'app-dashboard',standalone:true,imports:[PhasePlaceholderComponent],template:'<app-phase-placeholder title="Dashboard V2" detail="Dashboard behavior and API wiring will be migrated in Phase 5." />',changeDetection:ChangeDetectionStrategy.OnPush})
export class DashboardComponent {}
