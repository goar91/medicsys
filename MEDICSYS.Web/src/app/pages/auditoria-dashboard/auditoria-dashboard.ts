import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, NgFor, NgIf } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import {
  AcademicService,
  AuditoriaDashboard,
  AuditoriaEvent,
  AuditoriaEventsPage
} from '../../core/academic.service';
import { TopNavComponent } from '../../shared/top-nav/top-nav';

@Component({
  selector: 'app-auditoria-dashboard',
  standalone: true,
  imports: [TopNavComponent, NgIf, NgFor, DatePipe, ReactiveFormsModule],
  templateUrl: './auditoria-dashboard.html',
  styleUrl: './auditoria-dashboard.scss'
})
export class AuditoriaDashboardComponent implements OnInit {
  private readonly academicService = inject(AcademicService);

  readonly loading = signal(true);
  readonly loadingEvents = signal(true);
  readonly dashboard = signal<AuditoriaDashboard | null>(null);
  readonly events = signal<AuditoriaEvent[]>([]);
  readonly totalEvents = signal(0);
  readonly skip = signal(0);
  readonly take = 100;

  readonly roleFilter = new FormControl('', { nonNullable: true });
  readonly moduleFilter = new FormControl('', { nonNullable: true });
  readonly statusFromFilter = new FormControl<number | null>(400);
  readonly searchFilter = new FormControl('', { nonNullable: true });

  readonly availableRoles = ['Administrador', 'Auditoria', 'Profesor', 'Alumno', 'Odontologo', 'SinRol'];
  readonly availableModules = ['Academico', 'Odontologia', 'Autenticacion', 'Usuarios', 'FacturacionContabilidad', 'Clinico', 'Agenda', 'IA', 'Auditoria', 'General'];

  ngOnInit() {
    this.loadDashboard();
    this.loadEvents();
  }

  loadDashboard() {
    this.loading.set(true);
    this.academicService.getAuditoriaDashboard(30).subscribe({
      next: data => {
        this.dashboard.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.dashboard.set(null);
        this.loading.set(false);
      }
    });
  }

  loadEvents() {
    this.loadingEvents.set(true);
    this.academicService.getAuditoriaEventos({
      days: 30,
      role: this.roleFilter.value || undefined,
      module: this.moduleFilter.value || undefined,
      statusCodeFrom: this.statusFromFilter.value ?? undefined,
      search: this.searchFilter.value || undefined,
      skip: this.skip(),
      take: this.take
    }).subscribe({
      next: (page: AuditoriaEventsPage) => {
        this.events.set(page.items ?? []);
        this.totalEvents.set(page.total ?? 0);
        this.loadingEvents.set(false);
      },
      error: () => {
        this.events.set([]);
        this.totalEvents.set(0);
        this.loadingEvents.set(false);
      }
    });
  }

  applyFilters() {
    this.skip.set(0);
    this.loadEvents();
  }

  clearFilters() {
    this.roleFilter.setValue('');
    this.moduleFilter.setValue('');
    this.statusFromFilter.setValue(400);
    this.searchFilter.setValue('');
    this.skip.set(0);
    this.loadEvents();
  }

  refreshAll() {
    this.loadDashboard();
    this.loadEvents();
  }

  nextPage() {
    const next = this.skip() + this.take;
    if (next >= this.totalEvents()) {
      return;
    }
    this.skip.set(next);
    this.loadEvents();
  }

  prevPage() {
    const prev = this.skip() - this.take;
    this.skip.set(Math.max(0, prev));
    this.loadEvents();
  }
}
