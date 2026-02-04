import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe, NgFor, NgIf } from '@angular/common';
import { ComprasService } from '../../../../core/compras.service';
import { InventoryService } from '../../../../core/inventory.service';
import { PurchaseOrder, PurchaseItem, InventoryItem } from '../../../../core/models';

@Component({
  selector: 'app-compras',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, CurrencyPipe, DatePipe],
  templateUrl: './compras.html',
  styleUrl: './compras.scss'
})
export class ComprasComponent {
  private readonly fb = inject(FormBuilder);
  private readonly comprasService = inject(ComprasService);
  private readonly inventoryService = inject(InventoryService);

  readonly purchases = signal<PurchaseOrder[]>([]);
  readonly inventoryItems = signal<InventoryItem[]>([]);
  readonly loading = signal(false);
  readonly showModal = signal(false);
  readonly editingPurchase = signal<PurchaseOrder | null>(null);

  readonly selectedItems = signal<PurchaseItem[]>([]);
  readonly totalPurchase = computed(() =>
    this.selectedItems().reduce((sum, item) => sum + item.quantity * item.unitPrice, 0)
  );

  readonly purchaseForm = this.fb.group({
    supplier: ['', [Validators.required, Validators.minLength(2)]],
    invoiceNumber: [''],
    purchaseDate: [new Date().toISOString().split('T')[0], Validators.required],
    notes: ['']
  });

  readonly itemForm = this.fb.group({
    inventoryItemId: [0, [Validators.required, Validators.min(1)]],
    inventoryItemName: [''],
    quantity: [1, [Validators.required, Validators.min(1)]],
    unitPrice: [0, [Validators.required, Validators.min(0.01)]],
    expirationDate: ['']
  });

  readonly filterForm = this.fb.group({
    supplier: [''],
    dateFrom: [''],
    dateTo: [''],
    status: ['all']
  });

  constructor() {
    this.loadData();
  }

  loadData() {
    this.loading.set(true);

    this.comprasService.getAll().subscribe({
      next: purchases => {
        this.purchases.set(purchases);
        this.loading.set(false);
      },
      error: err => {
        console.error('Error al cargar compras:', err);
        this.loading.set(false);
      }
    });

    this.inventoryService.getAll().subscribe({
      next: items => this.inventoryItems.set(items)
    });
  }

  openModal() {
    this.showModal.set(true);
    this.editingPurchase.set(null);
    this.purchaseForm.reset({
      purchaseDate: new Date().toISOString().split('T')[0]
    });
    this.selectedItems.set([]);
  }

  closeModal() {
    this.showModal.set(false);
    this.editingPurchase.set(null);
    this.selectedItems.set([]);
    this.purchaseForm.reset();
    this.itemForm.reset();
  }

  addItem() {
    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }

    const formValue = this.itemForm.value;
    const selectedInventoryItem = this.inventoryItems().find(
      item => item.id === formValue.inventoryItemId
    );

    if (!selectedInventoryItem) return;

    const newItem: PurchaseItem = {
      inventoryItemId: formValue.inventoryItemId!,
      inventoryItemName: selectedInventoryItem.name,
      quantity: formValue.quantity!,
      unitPrice: formValue.unitPrice!,
      expirationDate: formValue.expirationDate || undefined
    };

    this.selectedItems.update(items => [...items, newItem]);
    this.itemForm.reset({ quantity: 1, unitPrice: 0 });
  }

  removeItem(index: number) {
    this.selectedItems.update(items => items.filter((_, i) => i !== index));
  }

  onInventoryItemChange(itemId: number) {
    const item = this.inventoryItems().find(i => i.id === itemId);
    if (item) {
      this.itemForm.patchValue({
        inventoryItemName: item.name,
        unitPrice: item.purchasePrice || 0
      });
    }
  }

  savePurchase() {
    if (this.purchaseForm.invalid || this.selectedItems().length === 0) {
      this.purchaseForm.markAllAsTouched();
      alert('Por favor complete todos los campos requeridos y agregue al menos un artículo.');
      return;
    }

    this.loading.set(true);
    const formValue = this.purchaseForm.value;

    const purchaseOrder: Partial<PurchaseOrder> = {
      supplier: formValue.supplier!,
      invoiceNumber: formValue.invoiceNumber || undefined,
      purchaseDate: formValue.purchaseDate!,
      notes: formValue.notes || undefined,
      items: this.selectedItems(),
      total: this.totalPurchase(),
      status: 'Received' // Auto-recibir y actualizar inventario
    };

    const saveOperation = this.editingPurchase()
      ? this.comprasService.update(this.editingPurchase()!.id, purchaseOrder)
      : this.comprasService.create(purchaseOrder);

    saveOperation.subscribe({
      next: () => {
        this.loadData();
        this.closeModal();
      },
      error: err => {
        console.error('Error al guardar compra:', err);
        alert('Error al guardar la compra. Por favor intente nuevamente.');
        this.loading.set(false);
      }
    });
  }

  editPurchase(purchase: PurchaseOrder) {
    this.editingPurchase.set(purchase);
    this.purchaseForm.patchValue({
      supplier: purchase.supplier,
      invoiceNumber: purchase.invoiceNumber || '',
      purchaseDate: purchase.purchaseDate.split('T')[0],
      notes: purchase.notes || ''
    });
    this.selectedItems.set([...purchase.items]);
    this.showModal.set(true);
  }

  deletePurchase(id: number) {
    if (!confirm('¿Está seguro de eliminar esta compra? Esta acción no se puede deshacer.')) {
      return;
    }

    this.loading.set(true);
    this.comprasService.delete(id).subscribe({
      next: () => this.loadData(),
      error: err => {
        console.error('Error al eliminar compra:', err);
        alert('Error al eliminar la compra.');
        this.loading.set(false);
      }
    });
  }

  applyFilters() {
    const filters = this.filterForm.value;
    this.comprasService.getAll(filters).subscribe({
      next: purchases => this.purchases.set(purchases)
    });
  }
}
