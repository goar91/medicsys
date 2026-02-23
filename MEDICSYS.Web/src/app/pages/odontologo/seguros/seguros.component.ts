import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { InsuranceClaim, OdontologoInnovationService } from '../../../core/odontologo-innovation.service';

@Component({
  selector: 'app-seguros',
  standalone: true,
  imports: [CommonModule, FormsModule, TopNavComponent],
  templateUrl: './seguros.component.html',
  styleUrl: './seguros.component.scss'
})
export class SegurosComponent implements OnInit {
  private readonly service = inject(OdontologoInnovationService);

  patients = signal<Array<{ id: string; fullName: string }>>([]);
  claims = signal<InsuranceClaim[]>([]);
  validationResult = signal<{
    insurer: string;
    policyNumber: string;
    procedureCode: string;
    requestedAmount: number;
    coveragePercent: number;
    coveredAmount: number;
    isApproved: boolean;
    message: string;
  } | null>(null);

  patientId = '';
  insurerName = 'IESS';
  policyNumber = '';
  procedureCode = '';
  procedureDescription = '';
  requestedAmount = 0;

  ngOnInit(): void {
    this.loadPatients();
    this.loadClaims();
  }

  loadPatients(): void {
    this.service.getPortalPatients().subscribe({
      next: patients => this.patients.set(patients.map(p => ({ id: p.id, fullName: p.fullName }))),
      error: err => console.error('Error loading patients', err)
    });
  }

  loadClaims(): void {
    this.service.getClaims().subscribe({
      next: claims => this.claims.set(claims),
      error: err => console.error('Error loading claims', err)
    });
  }

  validateCoverage(): void {
    if (!this.patientId) return;

    this.service.validateCoverage({
      patientId: this.patientId,
      insurerName: this.insurerName,
      policyNumber: this.policyNumber,
      procedureCode: this.procedureCode,
      requestedAmount: this.requestedAmount
    }).subscribe({
      next: result => this.validationResult.set(result),
      error: err => console.error('Error validating coverage', err)
    });
  }

  createClaim(): void {
    if (!this.patientId) return;

    this.service.createClaim({
      patientId: this.patientId,
      insurerName: this.insurerName,
      policyNumber: this.policyNumber,
      procedureCode: this.procedureCode,
      procedureDescription: this.procedureDescription || this.procedureCode,
      requestedAmount: this.requestedAmount
    }).subscribe({
      next: () => {
        this.validationResult.set(null);
        this.loadClaims();
      },
      error: err => console.error('Error creating claim', err)
    });
  }
}
