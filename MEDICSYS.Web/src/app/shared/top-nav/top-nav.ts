import { Component, computed, inject } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-top-nav',
  standalone: true,
  imports: [NgIf, NgFor, RouterLink, RouterLinkActive],
  templateUrl: './top-nav.html',
  styleUrl: './top-nav.scss'
})
export class TopNavComponent {
  private readonly auth = inject(AuthService);
  readonly user = this.auth.user;
  readonly role = computed(() => this.auth.getRole());
  readonly isProfessorRole = computed(() => this.role() === 'Profesor' || this.role() === 'Administrador');
  readonly professorLinks = [
    { label: 'Dashboard', path: '/professor/dashboard' },
    { label: 'Historias Clínicas', path: '/professor/histories' },
    { label: 'Pacientes', path: '/professor/patients' }
  ];
  readonly homeLink = computed(() => {
    const role = this.auth.getRole();
    if (role === 'Odontologo') {
      return '/odontologo/dashboard';
    }
    if (role === 'Profesor' || role === 'Administrador') {
      return '/professor';
    }
    return '/student';
  });

  logout() {
    this.auth.logout();
  }
}
