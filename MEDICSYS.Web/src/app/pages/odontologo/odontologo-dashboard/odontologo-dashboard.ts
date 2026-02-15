import { Component, DestroyRef, inject, signal, computed, OnInit } from '@angular/core';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { NgFor, NgIf } from '@angular/common';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { AgendaService, Appointment } from '../../../core/agenda.service';
import { PatientService } from '../../../core/patient.service';
import { AuthService } from '../../../core/auth.service';
import { InventoryService, InventoryAlert } from '../../../core/inventory.service';
import { AccountingService } from '../../../core/accounting.service';
import { ClinicalHistoryService } from '../../../core/clinical-history.service';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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
  private readonly destroyRef = inject(DestroyRef);
  private readonly agendaService = inject(AgendaService);
  private readonly patientService = inject(PatientService);
  private readonly auth = inject(AuthService);
  private readonly inventoryService = inject(InventoryService);
  private readonly accountingService = inject(AccountingService);
  private readonly clinicalHistoryService = inject(ClinicalHistoryService);

  readonly currentUserId = computed(() => this.auth.user()?.id ?? '');
  readonly appointments = signal<Appointment[]>([]);
  readonly totalPatients = signal<number>(0);
  readonly totalClinicalHistories = signal<number>(0);
  readonly totalInvoices = signal<number>(0);
  readonly monthlyRevenue = signal<number>(0);
  readonly inventoryAlerts = signal<InventoryAlert[]>([]);

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

    const unresolvedAlerts = this.inventoryAlerts().filter(a => !a.isResolved).length;

    return [
      {
        label: 'Citas Hoy',
        value: todayApptsCount,
        change: `${todayApptsCount} programadas`,
        trend: 'neutral' as const,
        icon: 'calendar'
      },
      {
        label: 'Historias Clínicas',
        value: this.totalClinicalHistories(),
        change: 'Total registradas',
        trend: 'up' as const,
        icon: 'history'
      },
      {
        label: 'Pacientes Activos',
        value: this.totalPatients(),
        change: 'Total registrados',
        trend: 'up' as const,
        icon: 'users'
      },
      {
        label: 'Ingresos del Mes',
        value: `$${this.monthlyRevenue().toFixed(2)}`,
        change: this.currentMonthLabel(),
        trend: 'up' as const,
        icon: 'dollar'
      },
      {
        label: 'Alertas Inventario',
        value: unresolvedAlerts,
        change: unresolvedAlerts > 0 ? 'Requieren atención' : 'Todo en orden',
        trend: unresolvedAlerts > 0 ? 'down' as const : 'neutral' as const,
        icon: 'alert'
      }
    ];
  });

  readonly quickActions = signal<QuickAction[]>([
    { label: 'Nueva Cita', route: '/odontologo/agenda', icon: 'calendar-plus', color: 'primary' },
    { label: 'Registrar Paciente', route: '/odontologo/pacientes', icon: 'user-plus', color: 'success' },
    { label: 'Ver Historias', route: '/odontologo/historias', icon: 'receipt', color: 'info' },
    { label: 'Nueva Factura', route: '/odontologo/facturacion/new', icon: 'receipt', color: 'warning' },
    { label: 'Contabilidad', route: '/odontologo/contabilidad', icon: 'package', color: 'info' },
    { label: 'Inventario', route: '/odontologo/inventario', icon: 'package', color: 'success' }
  ]);

  readonly recentAlerts = computed(() => {
    return this.inventoryAlerts()
      .filter(a => !a.isResolved)
      .slice(0, 5)
      .map(alert => ({
        type: this.getAlertTypeClass(alert.type),
        message: alert.message,
        time: this.getRelativeTime(alert.createdAt)
      }));
  });

  ngOnInit() {
    this.loadDashboardData();

    this.clinicalHistoryService.historyChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadClinicalHistories());

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((event) => {
        if (event.urlAfterRedirects.startsWith('/odontologo/dashboard')) {
          this.loadDashboardData();
        }
      });
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

    this.loadClinicalHistories();

    // Cargar alertas de inventario
    this.inventoryService.getAlerts(false).subscribe({
      next: (alerts) => {
        console.log('✅ Alertas de inventario cargadas:', alerts.length);
        this.inventoryAlerts.set(alerts);
      },
      error: (err) => {
        console.error('❌ Error al cargar alertas:', err);
        this.inventoryAlerts.set([]);
      }
    });

    // Cargar resumen contable del mes actual
    const now = new Date();
    const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0];
    const endOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().split('T')[0];
    
    this.accountingService.getSummary({ from: startOfMonth, to: endOfMonth }).subscribe({
      next: (summary) => {
        console.log('✅ Resumen contable cargado:', summary);
        this.monthlyRevenue.set(summary.totalIncome || 0);
      },
      error: (err) => {
        console.error('❌ Error al cargar resumen contable:', err);
        this.monthlyRevenue.set(0);
      }
    });
  }

  refreshDashboard() {
    this.loadDashboardData();
  }

  private loadClinicalHistories() {
    this.clinicalHistoryService.getAll().subscribe({
      next: (histories) => {
        this.totalClinicalHistories.set(histories.length);
      },
      error: (err) => {
        console.error('❌ Error al cargar historias clínicas:', err);
        this.totalClinicalHistories.set(0);
      }
    });
  }

  private currentMonthLabel() {
    return new Date().toLocaleDateString('es-EC', { month: 'long', year: 'numeric' });
  }

  private getAlertTypeClass(type: string): string {
    switch (type) {
      case 'OutOfStock':
      case 'Expired':
        return 'danger';
      case 'LowStock':
      case 'ExpirationWarning':
        return 'warning';
      default:
        return 'info';
    }
  }

  private getRelativeTime(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'ahora';
    if (diffMins < 60) return `${diffMins} min`;
    if (diffHours < 24) return `${diffHours}h`;
    if (diffDays < 7) return `${diffDays}d`;
    return date.toLocaleDateString('es-EC');
  }
}
