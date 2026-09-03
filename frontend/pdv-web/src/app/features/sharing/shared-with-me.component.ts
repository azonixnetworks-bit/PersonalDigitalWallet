import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PhasePlaceholderComponent } from '../../shared/components/phase-placeholder/phase-placeholder.component';
@Component({selector:'app-sharedwithme',standalone:true,imports:[PhasePlaceholderComponent],template:'<app-phase-placeholder title="Shared with me" detail="Secure sharing UI conversion is scheduled after vault migration." />',changeDetection:ChangeDetectionStrategy.OnPush})
export class SharedWithMeComponent {}
