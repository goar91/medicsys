import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { OdontologoInnovationService, PortalPatientSummary } from '../../../core/odontologo-innovation.service';

@Component({
  selector: 'app-portal-paciente',
  standalone: true,
  imports: [CommonModule, FormsModule, TopNavComponent],
  templateUrl: './portal-paciente.component.html',
  styleUrl: './portal-paciente.component.scss'
})
export class PortalPacienteComponent implements OnInit {
  private readonly service = inject(OdontologoInnovationService);

  patients = signal<Array<{ id: string; fullName: string; idNumber: string; phone: string; email: string }>>([]);
  summary = signal<PortalPatientSummary | null>(null);
  loading = signal(false);

  selectedPatientId = '';
  reminderSubject = 'Recordatorio MEDICSYS';
  reminderMessage = 'Le recordamos su próxima cita odontológica.';
  reminderDate = '';
  sendEmail = true;
  sendWhatsApp = true;

  ngOnInit(): void {
    this.loadPatients();
  }

  loadPatients(): void {
    this.service.getPortalPatients().subscribe({
      next: patients => this.patients.set(patients),
      error: err => console.error('Error loading portal patients', err)
    });
  }

  loadSummary(): void {
    if (!this.selectedPatientId) {
      this.summary.set(null);
      return;
    }

    this.loading.set(true);
    this.service.getPortalPatientSummary(this.selectedPatientId).subscribe({
      next: summary => {
        this.summary.set(summary);
        this.sendEmail = summary.preferences.emailEnabled;
        this.sendWhatsApp = summary.preferences.whatsappEnabled;
        this.loading.set(false);
      },
      error: err => {
        console.error('Error loading summary', err);
        this.loading.set(false);
      }
    });
  }

  updatePreferences(): void {
    const patientId = this.selectedPatientId;
    if (!patientId) return;

    this.service.updatePortalPreferences(patientId, this.sendEmail, this.sendWhatsApp).subscribe({
      next: () => this.loadSummary(),
      error: err => console.error('Error updating preferences', err)
    });
  }

  scheduleReminder(): void {
    const patientId = this.selectedPatientId;
    if (!patientId) return;

    this.service.scheduleReminder(patientId, {
      message: this.reminderMessage,
      subject: this.reminderSubject,
      scheduledFor: this.reminderDate || undefined,
      sendEmail: this.sendEmail,
      sendWhatsApp: this.sendWhatsApp
    }).subscribe({
      next: () => {
        this.reminderDate = '';
        this.loadSummary();
      },
      error: err => console.error('Error scheduling reminder', err)
    });
  }
}
