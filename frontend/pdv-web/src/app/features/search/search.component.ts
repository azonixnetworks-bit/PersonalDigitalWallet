import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PhasePlaceholderComponent } from '../../shared/components/phase-placeholder/phase-placeholder.component';
@Component({selector:'app-search',standalone:true,imports:[PhasePlaceholderComponent],template:'<app-phase-placeholder title="Search" detail="Search UI/API conversion is scheduled after vault migration." />',changeDetection:ChangeDetectionStrategy.OnPush})
export class SearchComponent {}
