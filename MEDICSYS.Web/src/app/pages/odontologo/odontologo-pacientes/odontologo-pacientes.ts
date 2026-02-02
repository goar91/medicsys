import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgFor, NgIf, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';

interface Paciente {
  id: string;
  nombre: string;
  cedula: string;
  telefono: string;
  email: string;
  ultimaVisita: string;
  proximaCita?: string;
  estado: 'activo' | 'inactivo';
  alertas: number;
}

@Component({
  selector: 'app-odontologo-pacientes',
  standalone: true,
  imports: [NgFor, NgIf, DatePipe, ReactiveFormsModule, TopNavComponent],
  templateUrl: './odontologo-pacientes.html',
  styleUrl: './odontologo-pacientes.scss'
})
export class OdontologoPacientesComponent {
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(false);
  readonly searchTerm = signal('');
  readonly showNewPatient = signal(false);

  readonly pacientes = signal<Paciente[]>([
    {
      id: '1',
      nombre: 'María González Pérez',
      cedula: '0102345678',
      telefono: '0987654321',
      email: 'maria.gonzalez@email.com',
      ultimaVisita: '2026-01-28',
      proximaCita: '2026-02-15',
      estado: 'activo',
      alertas: 0
    },
    {
      id: '2',
      nombre: 'Juan Carlos Ruiz',
      cedula: '0103456789',
      telefono: '0987654322',
      email: 'juan.ruiz@email.com',
      ultimaVisita: '2026-01-25',
      estado: 'activo',
      alertas: 1
    },
    {
      id: '3',
      nombre: 'Ana María Silva',
      cedula: '0104567890',
      telefono: '0987654323',
      email: 'ana.silva@email.com',
      ultimaVisita: '2025-12-20',
      proximaCita: '2026-02-05',
      estado: 'activo',
      alertas: 2
    }
  ]);

  readonly patientForm = this.fb.nonNullable.group({
    nombre: ['', [Validators.required]],
    cedula: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(10)]],
    telefono: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    fechaNacimiento: ['', [Validators.required]],
    direccion: ['', [Validators.required]],
    contactoEmergencia: [''],
    telefonoEmergencia: [''],
    alergias: [''],
    medicamentos: [''],
    enfermedades: ['']
  });

  filteredPacientes = signal<Paciente[]>([]);

  constructor() {
    this.filteredPacientes.set(this.pacientes());
  }

  filterPacientes(event: Event) {
    const term = (event.target as HTMLInputElement).value.toLowerCase();
    this.searchTerm.set(term);
    
    if (!term) {
      this.filteredPacientes.set(this.pacientes());
      return;
    }

    const filtered = this.pacientes().filter(p => 
      p.nombre.toLowerCase().includes(term) ||
      p.cedula.includes(term) ||
      p.email.toLowerCase().includes(term)
    );
    this.filteredPacientes.set(filtered);
  }

  toggleNewPatient() {
    this.showNewPatient.update(v => !v);
    if (!this.showNewPatient()) {
      this.patientForm.reset();
    }
  }

  savePatient() {
    if (this.patientForm.invalid) {
      this.patientForm.markAllAsTouched();
      return;
    }

    const formValue = this.patientForm.getRawValue();
    const newPatient: Paciente = {
      id: Date.now().toString(),
      nombre: formValue.nombre,
      cedula: formValue.cedula,
      telefono: formValue.telefono,
      email: formValue.email,
      ultimaVisita: new Date().toISOString().split('T')[0],
      estado: 'activo',
      alertas: 0
    };

    this.pacientes.update(list => [newPatient, ...list]);
    this.filteredPacientes.set(this.pacientes());
    this.toggleNewPatient();
  }
}
