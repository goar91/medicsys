import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, NgFor, NgIf } from '@angular/common';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { AccountingService } from '../../../core/accounting.service';
import { AccountingCategory, AccountingEntry, AccountingSummary } from '../../../core/models';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';

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
    this.accounting.createEntry({
      ...payload,
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
