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
  @Input() role: string = '';
  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<any>();
  @Output() delete = new EventEmitter<string>();

  readonly loading = signal(false);
  readonly patients = signal<Patient[]>([]);
  readonly professors = signal<UserSummary[]>([]);
  readonly students = signal<UserSummary[]>([]);
  readonly showNewPatient = signal(false);
  readonly selectedPatientId = signal<string | null>(null);

  readonly appointmentForm = this.fb.nonNullable.group({
    patientId: [''],
    patientName: ['', [Validators.required]],
    professorId: ['', [Validators.required]],
    studentId: [''],
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

  get providerLabel(): string {
    return this.role === 'Odontologo' ? 'Odontólogo' : 'Profesor';
  }

  ngOnInit() {
    this.loadPatients();
    this.loadProfessors();
    this.loadStudents();
    
    if (this.data) {
      this.initializeForm();
    }
  }

  initializeForm() {
    if (!this.data) return;

    const dateStr = this.data.date.toISOString().split('T')[0];
    const startTime = this.data.startAt ? new Date(this.data.startAt).toTimeString().substring(0, 5) : '09:00';
    const endTime = this.data.endAt ? new Date(this.data.endAt).toTimeString().substring(0, 5) : '10:00';

    // Para proveedores (Odontólogos), el professorId debe ser su propio ID
    const professorId = this.isProvider ? this.currentUserId : (this.data.professorId || '');

    this.appointmentForm.patchValue({
      patientName: this.data.patientName || '',
      professorId: professorId,
      studentId: this.data.studentId || '',
      reason: this.data.reason || 'Consulta odontológica',
      date: dateStr,
      startTime: startTime,
      endTime: endTime,
      status: (this.data as any).status || 'Pending',
      notes: this.data.notes || ''
    });
    
    console.log('📋 Formulario inicializado con professorId:', professorId);
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
    const request = this.role === 'Odontologo'
      ? this.agendaService.getOdontologos()
      : this.agendaService.getProfessors();
    request.subscribe({
      next: (profs) => this.professors.set(profs),
      error: (err) => console.error('Error loading professors:', err)
    });
  }

  loadStudents() {
    if (this.role !== 'Profesor') {
      return;
    }
    this.agendaService.getStudents().subscribe({
      next: (students) => this.students.set(students),
      error: (err) => console.error('Error loading students:', err)
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
        let message = 'Error al crear paciente';
        if (err?.error?.message) {
          message = err.error.message;
        } else if (typeof err?.error === 'string') {
          message = err.error;
        } else if (err?.message) {
          message = err.message;
        }
        alert(message);
        this.loading.set(false);
      }
    });
  }

  cancelNewPatient() {
    this.showNewPatient.set(false);
    this.newPatientForm.reset();
  }

  onSave() {
    if (this.appointmentForm.invalid) {
      this.appointmentForm.markAllAsTouched();
      return;
    }

    const formValue = this.appointmentForm.getRawValue();
    
    // Validar que haya un nombre de paciente
    if (!formValue.patientName || formValue.patientName.trim() === '') {
      alert('Debes seleccionar un paciente de la lista o ingresar el nombre del paciente');
      return;
    }
    
    // Profesor: requiere estudiante seleccionado
    // Alumno: usa su propio ID
    // Odontólogo: usa su propio ID (no requiere estudiante separado)
    let studentId: string;
    if (this.role === 'Profesor') {
      studentId = formValue.studentId;
      if (!studentId) {
        alert('Selecciona un alumno para la cita');
        return;
      }
    } else if (this.role === 'Alumno') {
      studentId = this.currentUserId;
    } else {
      // Odontólogo: puede enviar su propio ID o no enviar studentId
      studentId = this.currentUserId;
    }
    
    const professorId = this.isProvider ? this.currentUserId : formValue.professorId;

    if (!professorId) {
      alert('Error: Falta seleccionar el odontólogo/profesor');
      return;
    }
    
    const dateStr = formValue.date;
    const startDateTime = new Date(`${dateStr}T${formValue.startTime}:00`);
    const endDateTime = new Date(`${dateStr}T${formValue.endTime}:00`);

    const payload: any = {
      patientName: formValue.patientName,
      professorId: professorId,
      reason: formValue.reason,
      startAt: startDateTime.toISOString(),
      endAt: endDateTime.toISOString(),
      status: formValue.status,
      notes: formValue.notes || null
    };

    // Solo agregar studentId si está definido
    if (studentId) {
      payload.studentId = studentId;
    }

    console.log('📤 Enviando payload de cita:', payload);

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
