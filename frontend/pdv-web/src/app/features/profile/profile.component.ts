import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PhasePlaceholderComponent } from '../../shared/components/phase-placeholder/phase-placeholder.component';
@Component({selector:'app-profile',standalone:true,imports:[PhasePlaceholderComponent],template:'<app-phase-placeholder title="Profile" detail="Profile UI/API conversion is scheduled after core vault features." />',changeDetection:ChangeDetectionStrategy.OnPush})
export class ProfileComponent {}
