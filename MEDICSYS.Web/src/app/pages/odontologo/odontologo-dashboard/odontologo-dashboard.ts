import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgFor, NgIf } from '@angular/common';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { AgendaService, Appointment } from '../../../core/agenda.service';
import { PatientService } from '../../../core/patient.service';
import { AuthService } from '../../../core/auth.service';

interface DashboardMetric {
  label: string;
  value: number | string;
  change: string;
  trend: 'up' | 'down' | 'neutral';
  icon: string;
}

interface QuickAction {
  label: string;
  route: string;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-odontologo-dashboard',
  standalone: true,
  imports: [NgFor, NgIf, RouterLink, TopNavComponent],
  templateUrl: './odontologo-dashboard.html',
  styleUrl: './odontologo-dashboard.scss'
})
export class OdontologoDashboardComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly agendaService = inject(AgendaService);
  private readonly patientService = inject(PatientService);
  private readonly auth = inject(AuthService);

  readonly currentUserId = computed(() => this.auth.user()?.id ?? '');
  readonly appointments = signal<Appointment[]>([]);
  readonly totalPatients = signal<number>(0);

  readonly todayAppointments = computed(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    return this.appointments().filter(apt => {
      const aptDate = new Date(apt.startAt);
      return aptDate >= today && aptDate < tomorrow;
    }).map(apt => ({
      time: new Date(apt.startAt).toLocaleTimeString('es-EC', { hour: '2-digit', minute: '2-digit' }),
      patient: apt.patientName,
      treatment: apt.reason,
      status: apt.status.toLowerCase()
    }));
  });

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
        label: 'Citas Hoy',
        value: todayApptsCount,
        change: `${todayApptsCount} programadas`,
        trend: 'neutral' as const,
        icon: 'calendar'
      },
      {
        label: 'Pacientes Activos',
        value: this.totalPatients(),
        change: 'Total registrados',
        trend: 'up' as const,
        icon: 'users'
      },
      {
        label: 'Citas Totales',
        value: this.appointments().length,
        change: 'En el sistema',
        trend: 'neutral' as const,
        icon: 'calendar'
      },
      {
        label: 'Pendientes',
        value: this.appointments().filter(a => a.status === 'Pending').length,
        change: 'Por confirmar',
        trend: 'neutral' as const,
        icon: 'alert'
      }
    ];
  });

  readonly quickActions = signal<QuickAction[]>([
    { label: 'Nueva Cita', route: '/odontologo/agenda', icon: 'calendar-plus', color: 'primary' },
    { label: 'Registrar Paciente', route: '/odontologo/pacientes', icon: 'user-plus', color: 'success' },
    { label: 'Ver Historias', route: '/odontologo/historias', icon: 'receipt', color: 'info' },
    { label: 'Nueva Historia', route: '/odontologo/histories/new', icon: 'receipt', color: 'primary' },
    { label: 'Nueva Factura', route: '/odontologo/facturacion/new', icon: 'receipt', color: 'warning' },
    { label: 'Contabilidad', route: '/odontologo/contabilidad', icon: 'package', color: 'info' }
  ]);

  readonly recentAlerts = signal([
    { type: 'info', message: 'Dashboard actualizado con datos en tiempo real', time: 'ahora' }
  ]);

  ngOnInit() {
    this.loadDashboardData();
  }

  private loadDashboardData() {
    console.log('📊 Cargando datos del dashboard...');
    
    // Cargar citas
    this.agendaService.getAppointments({ professorId: this.currentUserId() }).subscribe({
      next: (appointments) => {
        console.log('✅ Citas cargadas:', appointments.length);
        this.appointments.set(appointments);
      },
      error: (err) => {
        console.error('❌ Error al cargar citas:', err);
      }
    });

    // Cargar pacientes
    this.patientService.getAll().subscribe({
      next: (patients) => {
        console.log('✅ Pacientes cargados:', patients.length);
        this.totalPatients.set(patients.length);
      },
      error: (err) => {
        console.error('❌ Error al cargar pacientes:', err);
      }
    });
  }
}
