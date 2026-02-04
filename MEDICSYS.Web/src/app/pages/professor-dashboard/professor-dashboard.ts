import { Component, OnInit, computed, signal, inject } from '@angular/core';
import { NgFor, NgIf, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
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
  selector: 'app-professor-dashboard',
  standalone: true,
  imports: [TopNavComponent, NgFor, NgIf, DatePipe, RouterLink],
  templateUrl: './professor-dashboard.html',
  styleUrl: './professor-dashboard.scss'
})
export class ProfessorDashboardComponent implements OnInit {
  private readonly academicService = inject(AcademicService);
  private readonly auth = inject(AuthService);

  readonly histories = signal<AcademicClinicalHistory[]>([]);
  readonly appointments = signal<AcademicAppointment[]>([]);
  readonly loading = signal(true);
  readonly filter = signal<'all' | 'Draft' | 'Submitted' | 'Approved' | 'Rejected'>('all');

  readonly currentUserId = computed(() => this.auth.user()?.id ?? '');

  readonly metrics = computed<DashboardMetric[]>(() => {
    const submitted = this.histories().filter(h => h.status === 'Submitted').length;
    const approved = this.histories().filter(h => h.status === 'Approved').length;
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
        label: 'Historias Pendientes',
        value: submitted,
        change: 'Esperan revisión',
        trend: submitted > 0 ? 'neutral' as const : 'up' as const,
        icon: 'clipboard'
      },
      {
        label: 'Historias Aprobadas',
        value: approved,
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
      },
      {
        label: 'Total Historias',
        value: this.histories().length,
        change: 'En el sistema',
        trend: 'neutral' as const,
        icon: 'file'
      }
    ];
  });

  readonly filtered = computed(() => {
    const value = this.filter();
    const items = this.histories();
    if (value === 'all') {
      return items;
    }
    return items.filter(item => item.status === value);
  });

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    
    // Cargar historias clínicas
    this.academicService.getClinicalHistories().subscribe({
      next: items => {
        this.histories.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.histories.set([]);
        this.loading.set(false);
      }
    });

    // Cargar citas
    this.academicService.getAppointments({ professorId: this.currentUserId() }).subscribe({
      next: items => {
        this.appointments.set(items);
      },
      error: () => {
        this.appointments.set([]);
      }
    });
  }

  setFilter(value: 'all' | 'Draft' | 'Submitted' | 'Approved' | 'Rejected') {
    this.filter.set(value);
  }

  delete(id: string) {
    if (!confirm('¿Seguro que deseas eliminar esta historia clínica?')) {
      return;
    }
    this.academicService.deleteClinicalHistory(id).subscribe({
      next: () => {
        this.histories.set(this.histories().filter(item => item.id !== id));
      },
      error: () => {
        // noop: could add toast
      }
    });
  }
}
