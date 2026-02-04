import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DecimalPipe, NgFor, NgIf } from '@angular/common';
import { DashboardService, DashboardStats } from '../../../core/dashboard.service';

interface ModuleCard {
  id: string;
  title: string;
  description: string;
  icon: string;
  route: string;
  color: string;
}

@Component({
  selector: 'app-contabilidad-dashboard',
  standalone: true,
  imports: [NgFor, NgIf, RouterLink, CurrencyPipe, DecimalPipe],
  templateUrl: './contabilidad-dashboard.html',
  styleUrl: './contabilidad-dashboard.scss'
})
export class ContabilidadDashboardComponent {
  private readonly dashboardService = inject(DashboardService);

  readonly stats = signal<DashboardStats | null>(null);
  readonly loading = signal(false);
  
  readonly currentMonth = new Date().toLocaleDateString('es-ES', { month: 'long', year: 'numeric' });
  
  readonly totalIncome = computed(() => this.stats()?.accounting.totalIncome || 0);
  readonly totalExpense = computed(() => this.stats()?.accounting.totalExpense || 0);
  readonly profit = computed(() => this.stats()?.accounting.profit || 0);
  readonly profitMargin = computed(() => this.stats()?.accounting.profitMargin || 0);
  
  readonly pendingInvoices = computed(() => this.stats()?.invoices.pending || 0);
  readonly pendingAmount = computed(() => this.stats()?.invoices.pendingAmount || 0);
  
  readonly lowStockItems = computed(() => this.stats()?.inventory.lowStockItems || 0);
  readonly inventoryValue = computed(() => this.stats()?.inventory.totalValue || 0);

  readonly modules: ModuleCard[] = [
    {
      id: 'ingresos',
      title: 'Ingresos',
      description: 'Registra y gestiona todos los ingresos de tu consultorio, incluyendo facturación de servicios.',
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <polyline points="23 6 13.5 15.5 8.5 10.5 1 18"/>
        <polyline points="17 6 23 6 23 12"/>
      </svg>`,
      route: '/odontologo/facturacion',
      color: 'green'
    },
    {
      id: 'gastos',
      title: 'Gastos',
      description: 'Controla todos los gastos operativos, salarios, servicios y otros egresos del consultorio.',
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <polyline points="23 18 13.5 8.5 8.5 13.5 1 6"/>
        <polyline points="17 18 23 18 23 12"/>
      </svg>`,
      route: '/odontologo/contabilidad/gastos',
      color: 'red'
    },
    {
      id: 'compras',
      title: 'Compras',
      description: 'Registra compras de materiales e insumos que alimentan automáticamente el inventario.',
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <circle cx="9" cy="21" r="1"/>
        <circle cx="20" cy="21" r="1"/>
        <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"/>
      </svg>`,
      route: '/odontologo/contabilidad/compras',
      color: 'purple'
    },
    {
      id: 'inventario',
      title: 'Inventario',
      description: 'Consulta el estado actual del inventario con alertas de stock bajo y vencimientos.',
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/>
      </svg>`,
      route: '/odontologo/inventario',
      color: 'cyan'
    },
    {
      id: 'reportes',
      title: 'Reportes Financieros',
      description: 'Genera reportes detallados de estado de resultados, flujo de caja y más.',
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
        <polyline points="14 2 14 8 20 8"/>
        <line x1="16" y1="13" x2="8" y2="13"/>
        <line x1="16" y1="17" x2="8" y2="17"/>
      </svg>`,
      route: '/odontologo/contabilidad/reportes',
      color: 'blue'
    },
    {
      id: 'cuentas',
      title: 'Cuentas por Cobrar',
      description: 'Gestiona facturas pendientes y pagos de clientes de forma eficiente.',
      icon: `<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <circle cx="12" cy="12" r="10"/>
        <polyline points="12 6 12 12 16 14"/>
      </svg>`,
      route: '/odontologo/facturacion',
      color: 'orange'
    }
  ];

  constructor() {
    this.loadData();
  }

  private loadData() {
    this.loading.set(true);
    const today = new Date();
    const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
    const lastDay = new Date(today.getFullYear(), today.getMonth() + 1, 0);

    this.dashboardService.getDashboardStats({ 
      from: firstDay.toISOString().split('T')[0], 
      to: lastDay.toISOString().split('T')[0] 
    }).subscribe({
      next: stats => {
        this.stats.set(stats);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading dashboard stats:', err);
        this.loading.set(false);
      }
    });
  }
}
