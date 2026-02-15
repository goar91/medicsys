import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportesService, FinancialReport, SalesReport, ComparativeReport } from '../../../../core/reportes.service';

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reportes.component.html',
  styleUrls: ['./reportes.component.scss']
})
export class ReportesComponent implements OnInit {
  private readonly reportesService = inject(ReportesService);

  activeTab = signal<'financial' | 'sales' | 'comparative'>('financial');
  loading = signal(false);

  financialReport = signal<FinancialReport | null>(null);
  salesReport = signal<SalesReport | null>(null);
  comparativeReport = signal<ComparativeReport | null>(null);

  // Filters for financial report
  financialStartDate = signal<string>('');
  financialEndDate = signal<string>('');

  // Filters for sales report
  salesStartDate = signal<string>('');
  salesEndDate = signal<string>('');

  // Filter for comparative
  comparativeMonths = signal<number>(12);

  // Computed max values for charts
  maxIncomeValue = computed(() => {
    const report = this.financialReport();
    if (!report) return 0;
    return this.getMaxValue(report.incomeByMonth.map((i: any) => i.amount));
  });

  maxExpenseCategoryValue = computed(() => {
    const report = this.financialReport();
    if (!report) return 0;
    return this.getMaxValue(report.expensesByCategory.map((c: any) => c.amount));
  });

  maxServiceRevenueValue = computed(() => {
    const report = this.salesReport();
    if (!report) return 0;
    return this.getMaxValue(report.topServices.map((s: any) => s.revenue));
  });

  maxPaymentValue = computed(() => {
    const report = this.salesReport();
    if (!report) return 0;
    return this.getMaxValue(report.paymentMethods.map((p: any) => p.amount));
  });

  ngOnInit() {
    this.loadFinancialReport();
  }

  setActiveTab(tab: 'financial' | 'sales' | 'comparative') {
    this.activeTab.set(tab);
    
    if (tab === 'financial' && !this.financialReport()) {
      this.loadFinancialReport();
    } else if (tab === 'sales' && !this.salesReport()) {
      this.loadSalesReport();
    } else if (tab === 'comparative' && !this.comparativeReport()) {
      this.loadComparativeReport();
    }
  }

  loadFinancialReport() {
    this.loading.set(true);
    this.reportesService.getFinancialReport(
      this.financialStartDate() || undefined,
      this.financialEndDate() || undefined
    ).subscribe({
      next: (data: FinancialReport) => {
        this.financialReport.set(data);
        this.loading.set(false);
      },
      error: (err: any) => {
        console.error('Error loading financial report:', err);
        this.loading.set(false);
      }
    });
  }

  loadSalesReport() {
    this.loading.set(true);
    this.reportesService.getSalesReport(
      this.salesStartDate() || undefined,
      this.salesEndDate() || undefined
    ).subscribe({
      next: (data: SalesReport) => {
        this.salesReport.set(data);
        this.loading.set(false);
      },
      error: (err: any) => {
        console.error('Error loading sales report:', err);
        this.loading.set(false);
      }
    });
  }

  loadComparativeReport() {
    this.loading.set(true);
    this.reportesService.getComparativeReport(this.comparativeMonths()).subscribe({
      next: (data: ComparativeReport) => {
        this.comparativeReport.set(data);
        this.loading.set(false);
      },
      error: (err: any) => {
        console.error('Error loading comparative report:', err);
        this.loading.set(false);
      }
    });
  }

  getMaxValue(values: number[]): number {
    return Math.max(...values, 0);
  }

  getBarHeight(value: number, max: number): number {
    return max > 0 ? (value / max) * 100 : 0;
  }

  getCategoryColor(index: number): string {
    const colors = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];
    return colors[index % colors.length];
  }

  exportToPDF() {
    alert('Funcionalidad de exportación a PDF en desarrollo');
  }

  exportToExcel() {
    alert('Funcionalidad de exportación a Excel en desarrollo');
  }
}
