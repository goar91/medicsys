import { Component, inject, signal, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgFor, NgIf } from '@angular/common';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';

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
export class OdontologoDashboardComponent {
  private readonly router = inject(Router);

  readonly metrics = signal<DashboardMetric[]>([
    {
      label: 'Citas Hoy',
      value: 8,
      change: '+2 vs ayer',
      trend: 'up',
      icon: 'calendar'
    },
    {
      label: 'Pacientes Activos',
      value: 156,
      change: '+12 este mes',
      trend: 'up',
      icon: 'users'
    },
    {
      label: 'Ingresos Mes',
      value: '$4,250',
      change: '+18% vs mes anterior',
      trend: 'up',
      icon: 'dollar'
    },
    {
      label: 'Alertas Pendientes',
      value: 3,
      change: '2 urgentes',
      trend: 'neutral',
      icon: 'alert'
    }
  ]);

  readonly quickActions = signal<QuickAction[]>([
    { label: 'Nueva Cita', route: '/odontologo/agenda', icon: 'calendar-plus', color: 'primary' },
    { label: 'Registrar Paciente', route: '/odontologo/pacientes', icon: 'user-plus', color: 'success' },
    { label: 'Nueva Factura', route: '/odontologo/facturacion/new', icon: 'receipt', color: 'warning' },
    { label: 'Ver Inventario', route: '/odontologo/inventario', icon: 'package', color: 'info' }
  ]);

  readonly todayAppointments = signal([
    { time: '09:00', patient: 'María González', treatment: 'Limpieza dental', status: 'confirmed' },
    { time: '10:30', patient: 'Juan Pérez', treatment: 'Endodoncia', status: 'confirmed' },
    { time: '14:00', patient: 'Ana Martínez', treatment: 'Consulta general', status: 'pending' },
    { time: '15:30', patient: 'Carlos Ruiz', treatment: 'Ortodoncia', status: 'confirmed' },
    { time: '17:00', patient: 'Laura Silva', treatment: 'Implante', status: 'pending' }
  ]);

  readonly recentAlerts = signal([
    { type: 'urgente', message: 'Stock bajo: Anestesia local (5 unidades)', time: '10 min' },
    { type: 'info', message: 'Cita de seguimiento: María González (mañana)', time: '1 hora' },
    { type: 'warning', message: 'Pago pendiente: Juan Pérez ($150)', time: '2 horas' }
  ]);
}
