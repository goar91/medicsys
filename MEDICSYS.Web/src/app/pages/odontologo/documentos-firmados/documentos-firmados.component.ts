import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { OdontologoInnovationService, SignedDocument } from '../../../core/odontologo-innovation.service';

@Component({
  selector: 'app-documentos-firmados',
  standalone: true,
  imports: [CommonModule, FormsModule, TopNavComponent],
  templateUrl: './documentos-firmados.component.html',
  styleUrl: './documentos-firmados.component.scss'
})
export class DocumentosFirmadosComponent implements OnInit {
  private readonly service = inject(OdontologoInnovationService);

  patients = signal<Array<{ id: string; fullName: string }>>([]);
  documents = signal<SignedDocument[]>([]);

  patientId = '';
  documentType = 'Receta';
  documentName = '';
  signatureProvider = 'FirmaEC';
  signatureSerial = '';
  documentContent = '';
  notes = '';
  validUntil = '';

  ngOnInit(): void {
    this.loadPatients();
    this.loadDocuments();
  }

  loadPatients(): void {
    this.service.getPortalPatients().subscribe({
      next: patients => this.patients.set(patients.map(p => ({ id: p.id, fullName: p.fullName }))),
      error: err => console.error('Error loading patients', err)
    });
  }

  loadDocuments(): void {
    this.service.getSignedDocuments().subscribe({
      next: docs => this.documents.set(docs),
      error: err => console.error('Error loading signed documents', err)
    });
  }

  createDocument(): void {
    if (!this.patientId || !this.documentName.trim()) {
      return;
    }

    this.service.createSignedDocument({
      patientId: this.patientId,
      documentType: this.documentType,
      documentName: this.documentName,
      signatureProvider: this.signatureProvider,
      signatureSerial: this.signatureSerial || undefined,
      documentContent: this.documentContent || undefined,
      notes: this.notes || undefined,
      validUntil: this.validUntil || undefined
    }).subscribe({
      next: () => {
        this.documentName = '';
        this.documentContent = '';
        this.notes = '';
        this.validUntil = '';
        this.loadDocuments();
      },
      error: err => console.error('Error creating signed document', err)
    });
  }
}
