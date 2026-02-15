import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgFor, NgIf } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { PatientService } from '../../../core/patient.service';
import { Patient, PatientCreateRequest } from '../../../core/patient.model';
import { ClinicalHistoryService } from '../../../core/clinical-history.service';
import { ClinicalHistory } from '../../../core/models';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-odontologo-pacientes',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, TopNavComponent],
  templateUrl: './odontologo-pacientes.html',
  styleUrl: './odontologo-pacientes.scss'
})
export class OdontologoPacientesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly historyService = inject(ClinicalHistoryService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly searchTerm = signal('');
  readonly showNewPatient = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedPatientId = signal<string | null>(null);

  readonly pacientes = signal<Patient[]>([]);

  readonly patientForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    idNumber: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(10)]],
    phone: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    dateOfBirth: ['', [Validators.required]],
    gender: ['', [Validators.required]],
    address: ['', [Validators.required]],
    emergencyContact: [''],
    emergencyPhone: [''],
    allergies: [''],
    medications: [''],
    diseases: [''],
    bloodType: ['']
  });

  filteredPacientes = signal<Patient[]>([]);

  ngOnInit() {
    this.loadPatients();
  }

  loadPatients() {
    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      patients: this.patientService.getAll(),
      histories: this.historyService.getAll().pipe(catchError(() => of([] as ClinicalHistory[])))
    }).subscribe({
      next: ({ patients, histories }) => {
        const merged = this.mergePatientsWithHistories(patients, histories);
        this.pacientes.set(merged);
        this.filteredPacientes.set(merged);
        this.loading.set(false);
      },
      error: (err: any) => {
        this.error.set(err.message || 'Error al cargar pacientes');
        this.loading.set(false);
      }
    });
  }

  filterPacientes(event: Event) {
    const term = (event.target as HTMLInputElement).value.toLowerCase();
    this.searchTerm.set(term);
    
    if (!term) {
      this.filteredPacientes.set(this.pacientes());
      return;
    }

    const filtered = this.pacientes().filter(p => 
      `${p.firstName} ${p.lastName}`.toLowerCase().includes(term) ||
      p.idNumber.toLowerCase().includes(term) ||
      (p.email?.toLowerCase() || '').includes(term)
    );
    this.filteredPacientes.set(filtered);
  }

  toggleNewPatient() {
    this.showNewPatient.update(v => !v);
    this.selectedPatientId.set(null);
    if (!this.showNewPatient()) {
      this.patientForm.reset();
    }
  }

  editPatient(patient: Patient) {
    this.selectedPatientId.set(patient.id);
    this.showNewPatient.set(true);
    this.patientForm.patchValue({
      firstName: patient.firstName,
      lastName: patient.lastName,
      idNumber: patient.idNumber,
      phone: patient.phone,
      email: patient.email || '',
      dateOfBirth: patient.dateOfBirth.split('T')[0],
      gender: patient.gender,
      address: patient.address,
      emergencyContact: patient.emergencyContact || '',
      emergencyPhone: patient.emergencyPhone || '',
      allergies: patient.allergies || '',
      medications: patient.medications || '',
      diseases: patient.diseases || '',
      bloodType: patient.bloodType || ''
    });
  }

  savePatient() {
    if (this.patientForm.invalid) {
      this.patientForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const formValue = this.patientForm.getRawValue();
    const patientData: PatientCreateRequest = {
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      idNumber: formValue.idNumber,
      dateOfBirth: formValue.dateOfBirth,
      gender: formValue.gender,
      address: formValue.address,
      phone: formValue.phone,
      email: formValue.email || undefined,
      emergencyContact: formValue.emergencyContact || undefined,
      emergencyPhone: formValue.emergencyPhone || undefined,
      allergies: formValue.allergies || undefined,
      medications: formValue.medications || undefined,
      diseases: formValue.diseases || undefined,
      bloodType: formValue.bloodType || undefined
    };

    const operation = this.selectedPatientId()
      ? this.patientService.update(this.selectedPatientId()!, patientData)
      : this.patientService.create(patientData);

    operation.subscribe({
      next: () => {
        this.loadPatients();
        this.toggleNewPatient();
        this.loading.set(false);
      },
      error: (err: any) => {
        this.error.set(err?.error?.message || err.message || 'Error al guardar paciente');
        this.loading.set(false);
      }
    });
  }

  deletePatient(id: string) {
    if (!confirm('¿Está seguro de eliminar este paciente?')) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.patientService.delete(id).subscribe({
      next: () => {
        this.loadPatients();
        this.loading.set(false);
      },
      error: (err: any) => {
        this.error.set(err.message || 'Error al eliminar paciente');
        this.loading.set(false);
      }
    });
  }

  getPatientAgeLabel(patient: Patient): string {
    if (patient.age && patient.age > 0) {
      return `${patient.age}`;
    }
    if (!patient.dateOfBirth) {
      return 'N/D';
    }
    const birthDate = new Date(patient.dateOfBirth);
    if (Number.isNaN(birthDate.getTime())) {
      return 'N/D';
    }
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age.toString();
  }

  getFullName(patient: Patient): string {
    return `${patient.firstName} ${patient.lastName}`;
  }

  navigateToHistory(patient: Patient) {
    if (patient.historyId) {
      this.router.navigate(['/odontologo/histories', patient.historyId]);
      return;
    }
    this.router.navigate(['/odontologo/historias'], { 
      queryParams: { idNumber: patient.idNumber } 
    });
  }

  navigateToAppointment(patient: Patient) {
    // Navegar a agenda con el paciente preseleccionado
    this.router.navigate(['/agenda'], { 
      queryParams: { 
        patientId: patient.id,
        patientName: this.getFullName(patient)
      } 
    });
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

      const age = Number(personal.age || 0);
      const firstName = (personal.firstName || 'Paciente').toString();
      const lastName = (personal.lastName || '').toString();
      const fallbackBirth = this.estimateBirthdate(age);

      map.set(idNumber, {
        id: history.id,
        firstName,
        lastName,
        idNumber,
        dateOfBirth: fallbackBirth,
        gender: (personal.gender || '').toString(),
        address: (personal.address || '').toString(),
        phone: (personal.phone || '').toString(),
        email: '',
        createdAt: history.createdAt,
        updatedAt: history.updatedAt,
        source: 'history',
        historyId: history.id,
        hasClinicalHistory: true,
        age: Number.isNaN(age) ? null : age
      });
    });

    return Array.from(map.values()).sort((a, b) => a.lastName.localeCompare(b.lastName));
  }

  private estimateBirthdate(age: number): string {
    if (!age || Number.isNaN(age)) {
      return '';
    }
    const today = new Date();
    const year = today.getFullYear() - age;
    return new Date(year, 0, 1).toISOString();
  }
}
