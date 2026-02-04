import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, NgFor, NgIf } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { AccountingService } from '../../../core/accounting.service';
import { AccountingCategory, AccountingEntry, AccountingSummary } from '../../../core/models';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';

interface ChartDataPoint {
  label: string;
  income: number;
  expense: number;
}

@Component({
  selector: 'app-odontologo-contabilidad',
  standalone: true,
  imports: [NgFor, NgIf, CurrencyPipe, DatePipe, ReactiveFormsModule, TopNavComponent],
  templateUrl: './odontologo-contabilidad.html',
  styleUrl: './odontologo-contabilidad.scss'
})
export class OdontologoContabilidadComponent {
  private readonly accounting = inject(AccountingService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly categories = signal<AccountingCategory[]>([]);
  readonly entries = signal<AccountingEntry[]>([]);
  readonly summary = signal<AccountingSummary | null>(null);
  readonly filterType = signal('');
  readonly fromDate = signal(this.formatDateInput(new Date(new Date().getFullYear(), new Date().getMonth(), 1)));
  readonly toDate = signal(this.formatDateInput(new Date()));
  
  // New features
  readonly editingEntry = signal<AccountingEntry | null>(null);
  readonly showDeleteConfirm = signal<string | null>(null);
  readonly viewMode = signal<'list' | 'chart'>('list');
  
  readonly chartData = computed<ChartDataPoint[]>(() => {
    const data = new Map<string, { income: number, expense: number }>();
    
    this.entries().forEach(entry => {
      const month = new Date(entry.date).toLocaleDateString('es-ES', { month: 'short', year: '2-digit' });
      if (!data.has(month)) {
        data.set(month, { income: 0, expense: 0 });
      }
      const point = data.get(month)!;
      if (entry.type === 'Income') {
        point.income += entry.amount;
      } else {
        point.expense += entry.amount;
      }
    });
    
    return Array.from(data.entries()).map(([label, values]) => ({
      label,
      income: values.income,
      expense: values.expense
    })).slice(-6); // Last 6 months
  });
  
  readonly maxChartValue = computed(() => {
    const data = this.chartData();
    if (data.length === 0) return 1000;
    return Math.max(...data.map(d => Math.max(d.income, d.expense))) * 1.1;
  });

  readonly entryForm = this.fb.nonNullable.group({
    type: ['Expense', Validators.required],
    categoryId: ['', Validators.required],
    date: [this.formatDateInput(new Date()), Validators.required],
    description: ['', Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    paymentMethod: ['Cash'],
    reference: ['']
  });

  readonly categoryTotals = computed(() => {
    const totals = new Map<string, number>();
    this.entries().forEach(entry => {
      totals.set(entry.categoryId, (totals.get(entry.categoryId) ?? 0) + entry.amount);
    });
    return totals;
  });

  constructor() {
    this.loadAll();
  }

  loadAll() {
    this.loading.set(true);
    this.accounting.getCategories().subscribe({
      next: categories => {
        this.categories.set(categories);
        this.refreshSummary();
        this.refreshEntries();
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  refreshEntries() {
    this.accounting.getEntries({
      from: this.fromDate(),
      to: this.toDate(),
      type: this.filterType() || undefined
    }).subscribe({
      next: entries => {
        this.entries.set(entries);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  refreshSummary() {
    this.accounting.getSummary({
      from: this.fromDate(),
      to: this.toDate()
    }).subscribe({
      next: summary => this.summary.set(summary)
    });
  }

  applyFilters() {
    this.refreshSummary();
    this.refreshEntries();
  }

  createEntry() {
    if (this.entryForm.invalid) {
      this.entryForm.markAllAsTouched();
      return;
    }

    const payload = this.entryForm.getRawValue();
    
    // If editing, update instead
    if (this.editingEntry()) {
      this.accounting.updateEntry(this.editingEntry()!.id, {
        ...payload,
        type: payload.type as 'Income' | 'Expense',
        paymentMethod: payload.paymentMethod || null,
        reference: payload.reference || null
      }).subscribe({
        next: () => {
          this.refreshEntries();
          this.refreshSummary();
          this.cancelEdit();
        }
      });
      return;
    }
    
    // Create new entry
    this.accounting.createEntry({
      ...payload,
      type: payload.type as 'Income' | 'Expense',
      paymentMethod: payload.paymentMethod || null,
      reference: payload.reference || null
    }).subscribe({
      next: entry => {
        this.entries.update(list => [entry, ...list]);
        this.refreshSummary();
        this.entryForm.patchValue({
          description: '',
          amount: 0,
          reference: ''
        });
      }
    });
  }
  
  editEntry(entry: AccountingEntry) {
    this.editingEntry.set(entry);
    this.entryForm.patchValue({
      type: entry.type,
      categoryId: entry.categoryId,
      date: entry.date,
      description: entry.description,
      amount: entry.amount,
      paymentMethod: entry.paymentMethod,
      reference: entry.reference || ''
    });
    
    // Scroll to form
    setTimeout(() => {
      document.querySelector('.form-card')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 100);
  }
  
  cancelEdit() {
    this.editingEntry.set(null);
    this.entryForm.patchValue({
      type: 'Expense',
      categoryId: '',
      date: this.formatDateInput(new Date()),
      description: '',
      amount: 0,
      paymentMethod: 'Cash',
      reference: ''
    });
  }
  
  confirmDelete(entryId: string) {
    this.showDeleteConfirm.set(entryId);
  }
  
  deleteEntry(entryId: string) {
    this.accounting.deleteEntry(entryId).subscribe({
      next: () => {
        this.entries.update(list => list.filter(e => e.id !== entryId));
        this.refreshSummary();
        this.showDeleteConfirm.set(null);
      }
    });
  }
  
  cancelDelete() {
    this.showDeleteConfirm.set(null);
  }
  
  toggleViewMode() {
    this.viewMode.update(mode => mode === 'list' ? 'chart' : 'list');
  }
  
  exportToCSV() {
    const headers = ['Fecha', 'Tipo', 'Categoría', 'Descripción', 'Monto', 'Método de Pago', 'Referencia'];
    const rows = this.entries().map(e => [
      e.date,
      e.type === 'Income' ? 'Ingreso' : 'Egreso',
      `${e.categoryGroup} - ${e.categoryName}`,
      e.description,
      e.amount.toString(),
      e.paymentMethod,
      e.reference || ''
    ]);
    
    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.map(cell => `"${cell}"`).join(','))
    ].join('\n');
    
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `contabilidad_${this.fromDate()}_${this.toDate()}.csv`;
    link.click();
  }
  
  getChartBarHeight(value: number): number {
    return (value / this.maxChartValue()) * 100;
  }

  categoryTotal(category: AccountingCategory) {
    return this.categoryTotals().get(category.id) ?? 0;
  }

  budgetPercent(category: AccountingCategory) {
    if (!category.monthlyBudget) return 0;
    return Math.min(100, Math.round((this.categoryTotal(category) / category.monthlyBudget) * 100));
  }

  private formatDateInput(date: Date) {
    const pad = (value: number) => value.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
  }
}
