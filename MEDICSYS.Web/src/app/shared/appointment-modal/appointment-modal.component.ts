import { Component, EventEmitter, Input, Output, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PatientService } from '../../core/patient.service';
import { Patient } from '../../core/patient.model';
import { AgendaService, UserSummary } from '../../core/agenda.service';
import { ClinicalHistoryService } from '../../core/clinical-history.service';
import { ClinicalHistory } from '../../core/models';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface AppointmentModalData {
  date: Date;
  appointmentId?: string;
  patientName?: string;
  reason?: string;
  notes?: string;
  startAt?: string;
  endAt?: string;
  studentId?: string;
  professorId?: string;
}

@Component({
  selector: 'app-appointment-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './appointment-modal.component.html',
  styleUrl: './appointment-modal.component.scss'
})
export class AppointmentModalComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly agendaService = inject(AgendaService);
  private readonly historyService = inject(ClinicalHistoryService);

  @Input() data: AppointmentModalData | null = null;
  @Input() isProvider = false;
  @Input() currentUserId = '';
  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<any>();
  @Output() delete = new EventEmitter<string>();

  readonly loading = signal(false);
  readonly patients = signal<Patient[]>([]);
  readonly professors = signal<UserSummary[]>([]);
  readonly showNewPatient = signal(false);
  readonly selectedPatientId = signal<string | null>(null);

  readonly appointmentForm = this.fb.nonNullable.group({
    patientId: [''],
    patientName: ['', [Validators.required]],
    professorId: ['', [Validators.required]],
    reason: ['Consulta odontológica', [Validators.required]],
    date: ['', [Validators.required]],
    startTime: ['09:00', [Validators.required]],
    endTime: ['10:00', [Validators.required]],
    status: ['Pending', [Validators.required]],
    notes: ['']
  });

  readonly newPatientForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    idNumber: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(10)]],
    phone: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    dateOfBirth: ['', [Validators.required]],
    gender: ['M', [Validators.required]],
    address: ['', [Validators.required]]
  });

  get isEditMode(): boolean {
    return !!this.data?.appointmentId;
  }

  get modalTitle(): string {
    return this.isEditMode ? 'Editar Cita' : 'Nueva Cita';
  }

  ngOnInit() {
    this.loadPatients();
    this.loadProfessors();
    
    if (this.data) {
      this.initializeForm();
    }
  }

  initializeForm() {
    if (!this.data) return;

    const dateStr = this.data.date.toISOString().split('T')[0];
    const startTime = this.data.startAt ? new Date(this.data.startAt).toTimeString().substring(0, 5) : '09:00';
    const endTime = this.data.endAt ? new Date(this.data.endAt).toTimeString().substring(0, 5) : '10:00';

    this.appointmentForm.patchValue({
      patientName: this.data.patientName || '',
      professorId: this.data.professorId || (this.isProvider ? this.currentUserId : ''),
      reason: this.data.reason || 'Consulta odontológica',
      date: dateStr,
      startTime: startTime,
      endTime: endTime,
      status: (this.data as any).status || 'Pending',
      notes: this.data.notes || ''
    });
  }

  loadPatients() {
    this.loading.set(true);
    forkJoin({
      patients: this.patientService.getAll().pipe(catchError(() => of([] as Patient[]))),
      histories: this.historyService.getAll().pipe(catchError(() => of([] as ClinicalHistory[])))
    }).subscribe({
      next: ({ patients, histories }) => {
        const merged = this.mergePatientsWithHistories(patients, histories);
        this.patients.set(merged);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading patients:', err);
        this.patients.set([]);
        this.loading.set(false);
      }
    });
  }
  loadProfessors() {
    this.agendaService.getOdontologos().subscribe({
      next: (profs) => this.professors.set(profs),
      error: (err) => console.error('Error loading professors:', err)
    });
  }

  onPatientSelect(event: Event) {
    const select = event.target as HTMLSelectElement;
    const patientId = select.value;

    if (patientId === 'new') {
      this.showNewPatient.set(true);
      this.appointmentForm.patchValue({ patientName: '' });
      this.selectedPatientId.set(null);
      return;
    }

    if (patientId) {
      const patient = this.patients().find(p => p.id === patientId);
      if (patient) {
        this.selectedPatientId.set(patientId);
        this.appointmentForm.patchValue({
          patientId: patient.id,
          patientName: `${patient.firstName} ${patient.lastName}`
        });
      }
    } else {
      this.selectedPatientId.set(null);
      this.appointmentForm.patchValue({ patientName: '' });
    }
  }

  saveNewPatient() {
    if (this.newPatientForm.invalid) {
      this.newPatientForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const formValue = this.newPatientForm.getRawValue();
    
    this.patientService.create({
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      idNumber: formValue.idNumber,
      phone: formValue.phone,
      email: formValue.email,
      dateOfBirth: formValue.dateOfBirth,
      gender: formValue.gender,
      address: formValue.address
    }).subscribe({
      next: (newPatient) => {
        const updatedPatients = [...this.patients(), newPatient];
        this.patients.set(updatedPatients);
        this.selectedPatientId.set(newPatient.id);
        this.appointmentForm.patchValue({
          patientId: newPatient.id,
          patientName: `${newPatient.firstName} ${newPatient.lastName}`
        });
        this.showNewPatient.set(false);
        this.newPatientForm.reset();
        this.loading.set(false);
      },
      error: (err: any) => {
        alert(err?.error?.message || err.message || 'Error al crear paciente');
        this.loading.set(false);
      }
    });
  }

  cancelNewPatient() {
    this.showNewPatient.set(false);
    this.newPatientForm.reset();
  }

  onSave() {
    console.log('💾 Formulario de cita - iniciando guardado');
    console.log('📝 Formulario válido:', !this.appointmentForm.invalid);
    console.log('📝 Valores del formulario:', this.appointmentForm.getRawValue());
    
    if (this.appointmentForm.invalid) {
      console.log('⚠️ Formulario inválido, marcando campos tocados');
      this.appointmentForm.markAllAsTouched();
      return;
    }

    const formValue = this.appointmentForm.getRawValue();
    // Para odontólogos, usamos el currentUserId como studentId para mantener compatibilidad con el backend
    const studentId = this.currentUserId;
    
    console.log('👤 Usuario actual (studentId):', studentId);
    console.log('🩺 Odontólogo:', formValue.professorId);
    
    if (!studentId) {
      console.log('❌ No se proporcionó usuario actual');
      alert('Error: No se pudo identificar el usuario actual');
      return;
    }
    
    const dateStr = formValue.date;
    const startDateTime = new Date(`${dateStr}T${formValue.startTime}:00`);
    const endDateTime = new Date(`${dateStr}T${formValue.endTime}:00`);

    const payload = {
      patientName: formValue.patientName,
      professorId: formValue.professorId,
      studentId, // Usamos currentUserId para compatibilidad con backend
      reason: formValue.reason,
      startAt: startDateTime.toISOString(),
      endAt: endDateTime.toISOString(),
      status: formValue.status,
      notes: formValue.notes || null,
      appointmentId: this.data?.appointmentId
    };

    console.log('📤 Emitiendo payload:', payload);
    this.save.emit(payload);
  }

  private mergePatientsWithHistories(patients: Patient[], histories: ClinicalHistory[]): Patient[] {
    const map = new Map<string, Patient>();
    patients.forEach(patient => {
      map.set(patient.idNumber, { ...patient, source: 'patient', hasClinicalHistory: false });
    });

    histories.forEach(history => {
      const data = history.data as any;
      const personal = data?.personal || {};
      const idNumber = (personal.idNumber || '').toString().trim();
      if (!idNumber) {
        return;
      }

      const existing = map.get(idNumber);
      if (existing) {
        map.set(idNumber, {
          ...existing,
          hasClinicalHistory: true,
          historyId: existing.historyId ?? history.id
        });
        return;
      }

      map.set(idNumber, {
        id: history.id,
        firstName: (personal.firstName || 'Paciente').toString(),
        lastName: (personal.lastName || '').toString(),
        idNumber,
        dateOfBirth: '',
        gender: (personal.gender || '').toString(),
        address: (personal.address || '').toString(),
        phone: (personal.phone || '').toString(),
        email: '',
        createdAt: history.createdAt,
        updatedAt: history.updatedAt,
        source: 'history',
        historyId: history.id,
        hasClinicalHistory: true
      });
    });

    return Array.from(map.values()).sort((a, b) => a.lastName.localeCompare(b.lastName));
  }

  onDelete() {
    if (!this.data?.appointmentId) return;
    if (!confirm('¿Está seguro de eliminar esta cita?')) return;
    
    this.delete.emit(this.data.appointmentId);
  }

  onClose() {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent) {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }
}
