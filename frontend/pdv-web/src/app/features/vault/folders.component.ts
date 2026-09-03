import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PhasePlaceholderComponent } from '../../shared/components/phase-placeholder/phase-placeholder.component';
@Component({selector:'app-folders',standalone:true,imports:[PhasePlaceholderComponent],template:'<app-phase-placeholder title="Folders" detail="Folder CRUD and secure upload folder selection will be migrated in Phase 6." />',changeDetection:ChangeDetectionStrategy.OnPush})
export class FoldersComponent {}
