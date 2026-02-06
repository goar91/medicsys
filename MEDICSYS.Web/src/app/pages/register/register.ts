import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/auth.service';

// PrimeNG imports
import { Select } from 'primeng/select';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { Button } from 'primeng/button';
import { Message } from 'primeng/message';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CommonModule,
    RouterLink,
    Select,
    InputText,
    Password,
    Button,
    Message
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly error = signal<string | null>(null);
  readonly loading = signal(false);
  readonly success = signal(false);

  readonly registerForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required]],
    universityId: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]]
  });

  readonly userTypes = [
    { 
      label: 'Estudiante', 
      value: 'Estudiante',
      description: 'Registro para estudiantes de medicina/odontología',
      icon: '🎓'
    }
  ];

  submitRegister() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const values = this.registerForm.getRawValue();
    
    // Validar que las contraseñas coincidan
    if (values.password !== values.confirmPassword) {
      this.error.set('Las contraseñas no coinciden');
      return;
    }

    this.error.set(null);
    this.loading.set(true);

    const payload = {
      fullName: values.fullName,
      universityId: values.universityId,
      email: values.email,
      password: values.password
    };

    this.auth.registerStudent(payload).subscribe({
      next: response => {
        this.success.set(true);
        this.loading.set(false);
        
        // Redirigir al login después de 2 segundos
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message || 'No se pudo completar el registro.');
      }
    });
  }

  get passwordsMatch(): boolean {
    const password = this.registerForm.get('password')?.value;
    const confirmPassword = this.registerForm.get('confirmPassword')?.value;
    return password === confirmPassword;
  }
}
