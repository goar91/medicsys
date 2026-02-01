import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgIf } from '@angular/common';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, NgIf],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly mode = signal<'login' | 'register'>('login');
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);

  readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  readonly registerForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required]],
    universityId: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  switchMode(mode: 'login' | 'register') {
    this.mode.set(mode);
    this.error.set(null);
  }

  submitLogin() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.loading.set(true);

    const { email, password } = this.loginForm.getRawValue();
    this.auth.login(email!, password!).subscribe({
      next: response => {
        this.auth.setSession(response);
        this.loading.set(false);
        this.redirectByRole(response.user.role);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Credenciales invalidas.');
      }
    });
  }

  submitRegister() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.loading.set(true);

    const payload = this.registerForm.getRawValue();
    this.auth.registerStudent(payload).subscribe({
      next: response => {
        this.auth.setSession(response);
        this.loading.set(false);
        this.router.navigate(['/student']);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('No se pudo completar el registro.');
      }
    });
  }

  private redirectByRole(role: string) {
    if (role === 'Profesor') {
      this.router.navigate(['/professor']);
    } else {
      this.router.navigate(['/student']);
    }
  }
}
