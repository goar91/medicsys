import { Component, OnInit, computed, signal, inject } from '@angular/core';
import { NgFor, NgIf, DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { TopNavComponent } from '../../shared/top-nav/top-nav';
import { AcademicService, AcademicClinicalHistory, AcademicAppointment } from '../../core/academic.service';
import { AuthService } from '../../core/auth.service';

interface DashboardMetric {
  label: string;
  value: number | string;
  change: string;
  trend: 'up' | 'down' | 'neutral';
  icon: string;
}

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [TopNavComponent, NgFor, NgIf, DatePipe, RouterLink],
  templateUrl: './student-dashboard.html',
  styleUrl: './student-dashboard.scss'
})
export class StudentDashboardComponent implements OnInit {
  private readonly academicService = inject(AcademicService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly histories = signal<AcademicClinicalHistory[]>([]);
  readonly appointments = signal<AcademicAppointment[]>([]);
  readonly loading = signal(true);

  readonly currentUserId = computed(() => this.auth.user()?.id ?? '');

  readonly draftCount = computed(() =>
    this.histories().filter(history => history.status === 'Draft').length
  );

  readonly submittedCount = computed(() =>
    this.histories().filter(history => history.status === 'Submitted').length
  );

  readonly approvedCount = computed(() =>
    this.histories().filter(history => history.status === 'Approved').length
  );

  readonly metrics = computed<DashboardMetric[]>(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    const todayApptsCount = this.appointments().filter(apt => {
      const aptDate = new Date(apt.startAt);
      return aptDate >= today && aptDate < tomorrow;
    }).length;

    return [
      {
        label: 'Borradores',
        value: this.draftCount(),
        change: 'Sin enviar',
        trend: 'neutral' as const,
        icon: 'edit'
      },
      {
        label: 'En Revisión',
        value: this.submittedCount(),
        change: 'Esperando aprobación',
        trend: 'neutral' as const,
        icon: 'clock'
      },
      {
        label: 'Aprobadas',
        value: this.approvedCount(),
        change: 'Total aprobadas',
        trend: 'up' as const,
        icon: 'check'
      },
      {
        label: 'Citas Hoy',
        value: todayApptsCount,
        change: 'Programadas',
        trend: 'neutral' as const,
        icon: 'calendar'
      }
    ];
  });

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    
    // Cargar historias clínicas del estudiante
    this.academicService.getClinicalHistories({ studentId: this.currentUserId() }).subscribe({
      next: items => {
        this.histories.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.histories.set([]);
        this.loading.set(false);
      }
    });

    // Cargar citas del estudiante
    this.academicService.getAppointments({ studentId: this.currentUserId() }).subscribe({
      next: items => {
        this.appointments.set(items);
      },
      error: () => {
        this.appointments.set([]);
      }
    });
  }

  newHistory() {
    this.router.navigate(['/student/histories/new']);
  }
}
