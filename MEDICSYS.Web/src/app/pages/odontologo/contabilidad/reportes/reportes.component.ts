import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportesService, FinancialReport, SalesReport, ComparativeReport, AdvancedReport } from '../../../../core/reportes.service';

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reportes.component.html',
  styleUrls: ['./reportes.component.scss']
})
export class ReportesComponent implements OnInit {
  private readonly reportesService = inject(ReportesService);

  activeTab = signal<'financial' | 'sales' | 'comparative' | 'advanced'>('financial');
  loading = signal(false);

  financialReport = signal<FinancialReport | null>(null);
  salesReport = signal<SalesReport | null>(null);
  comparativeReport = signal<ComparativeReport | null>(null);
  advancedReport = signal<AdvancedReport | null>(null);

  // Filters for financial report
  financialStartDate = signal<string>('');
  financialEndDate = signal<string>('');

  // Filters for sales report
  salesStartDate = signal<string>('');
  salesEndDate = signal<string>('');

  // Filter for comparative
  comparativeMonths = signal<number>(12);
  advancedStartDate = signal<string>('');
  advancedEndDate = signal<string>('');

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

  setActiveTab(tab: 'financial' | 'sales' | 'comparative' | 'advanced') {
    this.activeTab.set(tab);
    
    if (tab === 'financial' && !this.financialReport()) {
      this.loadFinancialReport();
    } else if (tab === 'sales' && !this.salesReport()) {
      this.loadSalesReport();
    } else if (tab === 'comparative' && !this.comparativeReport()) {
      this.loadComparativeReport();
    } else if (tab === 'advanced' && !this.advancedReport()) {
      this.loadAdvancedReport();
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

  loadAdvancedReport() {
    this.loading.set(true);
    this.reportesService.getAdvancedReport(
      this.advancedStartDate() || undefined,
      this.advancedEndDate() || undefined
    ).subscribe({
      next: (data: AdvancedReport) => {
        this.advancedReport.set(data);
        this.loading.set(false);
      },
      error: (err: any) => {
        console.error('Error loading advanced report:', err);
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
    window.print();
  }

  exportToExcel() {
    const rows = this.buildCsvRows();
    if (rows.length === 0) {
      return;
    }

    const csv = rows
      .map((row) => row.map((value) => this.escapeCsv(value)).join(','))
      .join('\r\n');

    const stamp = new Date().toISOString().replace(/[:T]/g, '-').split('.')[0];
    const fileName = `reporte-${this.activeTab()}-${stamp}.csv`;
    this.downloadFile(csv, fileName, 'text/csv;charset=utf-8;');
  }

  private buildCsvRows(): string[][] {
    const activeTab = this.activeTab();

    if (activeTab === 'financial') {
      const report = this.financialReport();
      if (!report) return [];

      const rows: string[][] = [
        ['Reporte', 'Financiero'],
        ['Periodo inicio', report.period.start],
        ['Periodo fin', report.period.end],
        ['Ingresos totales', report.summary.totalIncome.toFixed(2)],
        ['Gastos totales', report.summary.totalExpenses.toFixed(2)],
        ['Compras totales', report.summary.totalPurchases.toFixed(2)],
        ['Utilidad neta', report.summary.profit.toFixed(2)],
        ['Margen', report.summary.profitMargin.toFixed(2)],
        [],
        ['Mes', 'Ingreso', 'Gasto', 'Compra']
      ];

      report.incomeByMonth.forEach((income) => {
        const expense = report.expensesByMonth.find((item) => item.month === income.month)?.amount ?? 0;
        const purchase = report.purchasesByMonth.find((item) => item.month === income.month)?.amount ?? 0;
        rows.push([
          income.month,
          income.amount.toFixed(2),
          expense.toFixed(2),
          purchase.toFixed(2)
        ]);
      });

      return rows;
    }

    if (activeTab === 'sales') {
      const report = this.salesReport();
      if (!report) return [];

      const rows: string[][] = [
        ['Reporte', 'Ventas'],
        ['Periodo inicio', report.period.start],
        ['Periodo fin', report.period.end],
        ['Ventas totales', report.summary.totalSales.toFixed(2)],
        ['Facturas emitidas', report.summary.invoiceCount.toString()],
        ['Ticket promedio', report.summary.averageTicket.toFixed(2)],
        [],
        ['Servicio', 'Cantidad', 'Ingresos']
      ];

      report.topServices.forEach((service) => {
        rows.push([service.service, service.quantity.toString(), service.revenue.toFixed(2)]);
      });

      rows.push([]);
      rows.push(['Metodo de pago', 'Transacciones', 'Monto']);
      report.paymentMethods.forEach((payment) => {
        rows.push([payment.method, payment.count.toString(), payment.amount.toFixed(2)]);
      });

      return rows;
    }

    if (activeTab === 'comparative') {
      const report = this.comparativeReport();
      if (!report) return [];

      const rows: string[][] = [
        ['Reporte', 'Comparativo'],
        ['Meses', report.months.toString()],
        ['Ingreso promedio', report.averageIncome.toFixed(2)],
        ['Gasto promedio', report.averageExpenses.toFixed(2)],
        ['Utilidad promedio', report.averageProfit.toFixed(2)],
        [],
        ['Mes', 'Ingreso', 'Gasto', 'Utilidad']
      ];

      report.data.forEach((item) => {
        rows.push([
          item.monthName,
          item.income.toFixed(2),
          item.expenses.toFixed(2),
          item.profit.toFixed(2)
        ]);
      });

      return rows;
    }

    const report = this.advancedReport();
    if (!report) return [];

    const rows: string[][] = [
      ['Reporte', 'Avanzado'],
      ['Periodo inicio', report.period.start],
      ['Periodo fin', report.period.end],
      ['Ingresos totales', report.summary.totalRevenue.toFixed(2)],
      ['Costos operativos', report.summary.operationalCost.toFixed(2)],
      ['Utilidad bruta estimada', report.summary.estimatedGrossProfit.toFixed(2)],
      ['Margen bruto estimado', report.summary.estimatedGrossMargin.toFixed(2)],
      ['Pacientes nuevos', report.summary.newPatients.toString()],
      ['Gasto marketing', report.summary.marketingExpense.toFixed(2)],
      ['Costo adquisicion paciente', report.summary.patientAcquisitionCost.toFixed(2)],
      ['LTV', report.summary.ltv.toFixed(2)],
      [],
      ['Procedimiento', 'Cantidad', 'Ingreso', 'Costo', 'Utilidad', 'Margen']
    ];

    report.profitabilityByProcedure.forEach((item) => {
      rows.push([
        item.procedure,
        item.quantity.toString(),
        item.revenue.toFixed(2),
        item.estimatedCost.toFixed(2),
        item.estimatedProfit.toFixed(2),
        item.marginPercent.toFixed(2)
      ]);
    });

    rows.push([]);
    rows.push(['Cliente', 'Identificacion', 'Ingresos', 'Facturas', 'Ticket promedio']);
    report.customerLifetimeValue.forEach((item) => {
      rows.push([
        item.customerName,
        item.customerIdentification,
        item.revenue.toFixed(2),
        item.invoiceCount.toString(),
        item.averageTicket.toFixed(2)
      ]);
    });

    return rows;
  }

  private escapeCsv(value: string): string {
    const normalized = value.replace(/"/g, '""');
    return /[",\r\n]/.test(normalized) ? `"${normalized}"` : normalized;
  }

  private downloadFile(content: string, fileName: string, mimeType: string): void {
    const blob = new Blob([content], { type: mimeType });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }
}
