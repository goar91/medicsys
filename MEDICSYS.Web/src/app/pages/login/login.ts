import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/auth.service';

// PrimeNG imports
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CommonModule,
    InputText,
    Password,
    Button,
    Message
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly error = signal<string | null>(null);
  readonly loading = signal(false);

  readonly loginForm = this.fb.nonNullable.group({
    userType: ['Alumno', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  readonly userTypes = [
    {
      label: 'Estudiante',
      value: 'Alumno',
      description: 'Acceso para estudiantes de medicina/odontología',
      svgIcon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 10v6M2 10l10-5 10 5-10 5z"/><path d="M6 12v5c3 3 9 3 12 0v-5"/></svg>'
    },
    {
      label: 'Profesor',
      value: 'Profesor',
      description: 'Supervisión y revisión académica',
      svgIcon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="2" y="7" width="20" height="14" rx="2"/><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/></svg>'
    },
    {
      label: 'Odontólogo',
      value: 'Odontologo',
      description: 'Gestión de pacientes y tratamientos',
      svgIcon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3z"/><path d="M12 12v10"/><path d="M9 12v5l-1.5 5M15 12v5l1.5 5"/></svg>'
    }
  ];

  get selectedUserType(): string {
    return this.loginForm.get('userType')?.value ?? '';
  }

  get emailPlaceholder(): string {
    switch (this.selectedUserType) {
      case 'Profesor':
        return 'profesor@medicsys.com';
      case 'Odontologo':
        return 'odontologo@medicsys.com';
      default:
        return 'estudiante@medicsys.com';
    }
  }

  selectUserType(value: string) {
    this.loginForm.patchValue({ userType: value });
  }

  submitLogin() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.loading.set(true);

    const { email, password, userType } = this.loginForm.getRawValue();
    this.auth.login(email!, password!).subscribe({
      next: response => {
        this.loading.set(false);
        // Verificar si el rol seleccionado coincide con el rol del usuario
        if (!this.isRoleMatch(userType, response.user.role)) {
          this.error.set(`Tu usuario pertenece al rol "${response.user.role}". Selecciona ese rol para continuar.`);
          return;
        }
        // Solo guardar la sesión si el rol coincide
        this.auth.setSession(response);
        this.redirectByRole(response.user.role);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Credenciales inválidas. Verifica tu correo y contraseña.');
      }
    });
  }

  private redirectByRole(role: string) {
    switch (role) {
      case 'Odontologo':
        this.router.navigate(['/odontologo/dashboard']);
        break;
      case 'Profesor':
        this.router.navigate(['/professor']);
        break;
      case 'Alumno':
      default:
        this.router.navigate(['/student']);
        break;
    }
  }

  private isRoleMatch(selected: string, actual: string): boolean {
    return selected === actual;
  }
}
