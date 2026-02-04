import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { GastosService } from '../../../../core/gastos.service';
import { Expense, ExpenseSummary } from '../../../../core/models';

@Component({
  selector: 'app-gastos',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './gastos.component.html',
  styleUrls: ['./gastos.component.scss']
})
export class GastosComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly gastosService = inject(GastosService);

  expenses = signal<Expense[]>([]);
  summary = signal<ExpenseSummary | null>(null);
  showModal = signal(false);
  isEditing = signal(false);
  currentExpenseId = signal<string | null>(null);
  loading = signal(false);

  expenseForm!: FormGroup;

  // Filters
  filterCategory = signal<string>('');
  filterPaymentMethod = signal<string>('');
  filterStartDate = signal<string>('');
  filterEndDate = signal<string>('');

  // Categories
  categories = [
    { value: 'Supplies', label: 'Insumos' },
    { value: 'Equipment', label: 'Equipamiento' },
    { value: 'Maintenance', label: 'Mantenimiento' },
    { value: 'Utilities', label: 'Servicios (Luz, Agua, Internet)' },
    { value: 'Rent', label: 'Alquiler' },
    { value: 'Salaries', label: 'Salarios' },
    { value: 'Marketing', label: 'Marketing' },
    { value: 'Professional', label: 'Servicios Profesionales' },
    { value: 'Other', label: 'Otros' }
  ];

  paymentMethods = [
    { value: 'Efectivo', label: 'Efectivo' },
    { value: 'Tarjeta', label: 'Tarjeta' },
    { value: 'Transferencia', label: 'Transferencia' }
  ];

  filteredExpenses = computed(() => {
    let filtered = this.expenses();

    if (this.filterCategory()) {
      filtered = filtered.filter(e => e.category === this.filterCategory());
    }

    if (this.filterPaymentMethod()) {
      filtered = filtered.filter(e => e.paymentMethod === this.filterPaymentMethod());
    }

    if (this.filterStartDate()) {
      filtered = filtered.filter(e => new Date(e.expenseDate) >= new Date(this.filterStartDate()));
    }

    if (this.filterEndDate()) {
      filtered = filtered.filter(e => new Date(e.expenseDate) <= new Date(this.filterEndDate()));
    }

    return filtered;
  });

  totalExpenses = computed(() => {
    return this.filteredExpenses().reduce((sum, e) => sum + e.amount, 0);
  });

  ngOnInit() {
    this.initForm();
    this.loadExpenses();
    this.loadSummary();
  }

  initForm() {
    this.expenseForm = this.fb.group({
      description: ['', [Validators.required, Validators.maxLength(200)]],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      expenseDate: [new Date().toISOString().split('T')[0], Validators.required],
      category: ['', Validators.required],
      paymentMethod: ['Efectivo', Validators.required],
      invoiceNumber: [''],
      supplier: [''],
      notes: ['']
    });
  }

  loadExpenses() {
    this.loading.set(true);
    const filters = {
      startDate: this.filterStartDate() || undefined,
      endDate: this.filterEndDate() || undefined,
      category: this.filterCategory() || undefined,
      paymentMethod: this.filterPaymentMethod() || undefined
    };

    this.gastosService.getAll(filters).subscribe({
      next: (data: Expense[]) => {
        this.expenses.set(data);
        this.loading.set(false);
      },
      error: (err: any) => {
        console.error('Error loading expenses:', err);
        this.loading.set(false);
      }
    });
  }

  loadSummary() {
    this.gastosService.getSummary().subscribe({
      next: (data: ExpenseSummary) => this.summary.set(data),
      error: (err: any) => console.error('Error loading summary:', err)
    });
  }

  openCreateModal() {
    this.isEditing.set(false);
    this.currentExpenseId.set(null);
    this.expenseForm.reset({
      expenseDate: new Date().toISOString().split('T')[0],
      paymentMethod: 'Efectivo'
    });
    this.showModal.set(true);
  }

  openEditModal(expense: Expense) {
    this.isEditing.set(true);
    this.currentExpenseId.set(expense.id);
    this.expenseForm.patchValue({
      description: expense.description,
      amount: expense.amount,
      expenseDate: new Date(expense.expenseDate).toISOString().split('T')[0],
      category: expense.category,
      paymentMethod: expense.paymentMethod,
      invoiceNumber: expense.invoiceNumber,
      supplier: expense.supplier,
      notes: expense.notes
    });
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.expenseForm.reset();
  }

  saveExpense() {
    if (this.expenseForm.invalid) return;

    const formValue = this.expenseForm.value;
    const expenseData = {
      ...formValue,
      expenseDate: new Date(formValue.expenseDate).toISOString()
    };

    if (this.isEditing() && this.currentExpenseId()) {
      this.gastosService.update(this.currentExpenseId()!, expenseData).subscribe({
        next: () => {
          this.closeModal();
          this.loadExpenses();
          this.loadSummary();
        },
        error: (err: any) => console.error('Error updating expense:', err)
      });
    } else {
      this.gastosService.create(expenseData).subscribe({
        next: () => {
          this.closeModal();
          this.loadExpenses();
          this.loadSummary();
        },
        error: (err: any) => console.error('Error creating expense:', err)
      });
    }
  }

  deleteExpense(id: string) {
    if (!confirm('¿Está seguro de eliminar este gasto?')) return;

    this.gastosService.delete(id).subscribe({
      next: () => {
        this.loadExpenses();
        this.loadSummary();
      },
      error: (err: any) => console.error('Error deleting expense:', err)
    });
  }

  getCategoryLabel(value: string): string {
    return this.categories.find(c => c.value === value)?.label || value;
  }

  clearFilters() {
    this.filterCategory.set('');
    this.filterPaymentMethod.set('');
    this.filterStartDate.set('');
    this.filterEndDate.set('');
    this.loadExpenses();
  }
}
