import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PhasePlaceholderComponent } from '../../shared/components/phase-placeholder/phase-placeholder.component';
@Component({selector:'app-documents',standalone:true,imports:[PhasePlaceholderComponent],template:'<app-phase-placeholder title="Documents" detail="Document upload/encryption progress will be migrated in Phase 6." />',changeDetection:ChangeDetectionStrategy.OnPush})
export class DocumentsComponent {}
