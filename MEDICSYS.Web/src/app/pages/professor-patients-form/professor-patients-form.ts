import { Component, OnInit, signal, inject } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TopNavComponent } from '../../shared/top-nav/top-nav';
import { AcademicService, AcademicPatient } from '../../core/academic.service';

@Component({
  selector: 'app-professor-patients-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TopNavComponent],
  templateUrl: './professor-patients-form.html',
  styleUrl: './professor-patients-form.scss'
})
export class ProfessorPatientsFormComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly academicService = inject(AcademicService);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly patientId = signal<string | null>(null);
  readonly isEditMode = signal(false);

  form!: FormGroup;

  ngOnInit() {
    this.initForm();
    
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.patientId.set(id);
      this.isEditMode.set(true);
      this.loadPatient(id);
    }
  }

  initForm() {
    this.form = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      idNumber: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
      dateOfBirth: ['', Validators.required],
      gender: ['', Validators.required],
      phone: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
      email: ['', [Validators.email]],
      address: [''],
      bloodType: [''],
      allergies: [''],
      medicalConditions: [''],
      emergencyContact: ['', Validators.required],
      emergencyPhone: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]]
    });
  }

  loadPatient(id: string) {
    this.loading.set(true);
    this.error.set(null);

    this.academicService.getPatientById(id).subscribe({
      next: (patient) => {
        // Format date for input
        const dateOfBirth = patient.dateOfBirth ? 
          new Date(patient.dateOfBirth).toISOString().substring(0, 10) : '';
        
        this.form.patchValue({
          ...patient,
          dateOfBirth
        });
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Error al cargar el paciente');
        this.loading.set(false);
      }
    });
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const patientData = this.form.value;

    const request = this.isEditMode()
      ? this.academicService.updatePatient(this.patientId()!, patientData)
      : this.academicService.createPatient(patientData);

    request.subscribe({
      next: () => {
        this.router.navigate(['/professor/dashboard']);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Error al guardar el paciente');
        this.saving.set(false);
      }
    });
  }

  cancel() {
    this.router.navigate(['/professor/dashboard']);
  }

  getFieldError(fieldName: string): string | null {
    const field = this.form.get(fieldName);
    if (!field || !field.touched || !field.errors) {
      return null;
    }

    if (field.errors['required']) {
      return 'Este campo es requerido';
    }
    if (field.errors['minlength']) {
      return `Mínimo ${field.errors['minlength'].requiredLength} caracteres`;
    }
    if (field.errors['pattern']) {
      if (fieldName === 'idNumber') {
        return 'Debe ser una cédula válida de 10 dígitos';
      }
      if (fieldName === 'phone' || fieldName === 'emergencyPhone') {
        return 'Debe ser un número de 10 dígitos';
      }
    }
    if (field.errors['email']) {
      return 'Debe ser un email válido';
    }

    return 'Campo inválido';
  }
}
