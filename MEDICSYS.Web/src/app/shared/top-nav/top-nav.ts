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
  readonly isProfessorRole = computed(() => {
    const role = this.role();
    if (role === 'Profesor') {
      return true;
    }
    if (role === 'Administrador') {
      return !window.location.pathname.startsWith('/auditoria');
    }
    return false;
  });
  readonly isAuditoriaRole = computed(() => {
    const role = this.role();
    if (role === 'Auditoria') {
      return true;
    }
    return role === 'Administrador' && window.location.pathname.startsWith('/auditoria');
  });
  readonly professorLinks = [
    { label: 'Dashboard', path: '/professor/dashboard' },
    { label: 'Historias Clínicas', path: '/professor/histories' },
    { label: 'Pacientes', path: '/professor/patients' }
  ];
  readonly auditoriaLinks = [
    { label: 'Dashboard Auditoría', path: '/auditoria' }
  ];
  readonly homeLink = computed(() => {
    const role = this.auth.getRole();
    if (role === 'Odontologo') {
      return '/odontologo/dashboard';
    }
    if (role === 'Auditoria') {
      return '/auditoria';
    }
    if (role === 'Administrador' && window.location.pathname.startsWith('/auditoria')) {
      return '/auditoria';
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
