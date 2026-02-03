import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgFor, NgIf, DatePipe, DecimalPipe } from '@angular/common';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { AccountingService } from '../../../core/accounting.service';
import { AccountingEntry, AccountingSummary } from '../../../core/models';

interface QuickStat {
  label: string;
  value: number;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-contabilidad-dashboard',
  standalone: true,
  imports: [NgFor, NgIf, RouterLink, DatePipe, DecimalPipe, TopNavComponent],
  templateUrl: './contabilidad-dashboard.html',
  styleUrl: './contabilidad-dashboard.scss'
})
export class ContabilidadDashboardComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly accountingService = inject(AccountingService);

  readonly loading = signal(false);
  readonly summary = signal<AccountingSummary | null>(null);
  readonly recentEntries = signal<AccountingEntry[]>([]);

  readonly stats = signal<QuickStat[]>([
    { label: 'Ingresos del Mes', value: 0, icon: '💰', color: 'success' },
    { label: 'Gastos del Mes', value: 0, icon: '💸', color: 'danger' },
    { label: 'Balance', value: 0, icon: '📊', color: 'info' },
    { label: 'Transacciones', value: 0, icon: '📝', color: 'primary' }
  ]);

  readonly modules = signal([
    {
      title: 'Ingresos',
      description: 'Registra y consulta todos los ingresos',
      route: '/odontologo/contabilidad/ingresos',
      icon: '💵',
      color: 'success'
    },
    {
      title: 'Gastos',
      description: 'Administra gastos y egresos',
      route: '/odontologo/contabilidad/gastos',
      icon: '🧾',
      color: 'danger'
    },
    {
      title: 'Reportes',
      description: 'Genera reportes y análisis',
      route: '/odontologo/contabilidad/reportes',
      icon: '📈',
      color: 'info'
    },
    {
      title: 'Categorías',
      description: 'Gestiona categorías contables',
      route: '/odontologo/contabilidad/categorias',
      icon: '🏷️',
      color: 'warning'
    }
  ]);

  ngOnInit() {
    this.loadData();
  }

  private loadData() {
    this.loading.set(true);

    // Obtener fecha actual
    const today = new Date();
    const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
    const from = firstDay.toISOString().split('T')[0];

    // Cargar resumen
    this.accountingService.getSummary({ from }).subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.updateStats(summary);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error al cargar resumen:', err);
        this.loading.set(false);
      }
    });

    // Cargar entradas recientes
    this.accountingService.getEntries({ from }).subscribe({
      next: (entries) => {
        this.recentEntries.set(entries.slice(0, 10));
      },
      error: (err) => {
        console.error('Error al cargar entradas:', err);
      }
    });
  }

  private updateStats(summary: AccountingSummary) {
    const stats = this.stats();
    stats[0].value = summary.totalIncome;
    stats[1].value = summary.totalExpense;
    stats[2].value = summary.net;
    stats[3].value = this.recentEntries().length;
    this.stats.set([...stats]);
  }

  navigateTo(route: string) {
    this.router.navigate([route]);
  }
}
