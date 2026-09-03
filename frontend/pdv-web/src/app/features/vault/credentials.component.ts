import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PhasePlaceholderComponent } from '../../shared/components/phase-placeholder/phase-placeholder.component';
@Component({selector:'app-credentials',standalone:true,imports:[PhasePlaceholderComponent],template:'<app-phase-placeholder title="Credentials" detail="Credential vault conversion is scheduled after document/folder parity." />',changeDetection:ChangeDetectionStrategy.OnPush})
export class CredentialsComponent {}
