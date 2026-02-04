import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { NgFor, NgIf, DatePipe, DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { InventoryService, InventoryItem, InventoryAlert } from '../../../core/inventory.service';

@Component({
  selector: 'app-odontologo-inventario',
  standalone: true,
  imports: [TopNavComponent, NgFor, NgIf, DatePipe, DecimalPipe, ReactiveFormsModule],
  templateUrl: './odontologo-inventario.html',
  styleUrl: './odontologo-inventario.scss'
})
export class OdontologoInventarioComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly fb = inject(FormBuilder);

  readonly items = signal<InventoryItem[]>([]);
  readonly alerts = signal<InventoryAlert[]>([]);
  readonly loading = signal(true);
  readonly filter = signal<'all' | 'low-stock' | 'expiring'>('all');
  readonly showNewItemForm = signal(false);
  readonly editingItem = signal<InventoryItem | null>(null);

  readonly itemForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    description: [''],
    sku: [''],
    quantity: [0, [Validators.required, Validators.min(0)]],
    minimumQuantity: [0, [Validators.required, Validators.min(0)]],
    unitPrice: [0, [Validators.required, Validators.min(0)]],
    supplier: [''],
    expirationDate: ['']
  });

  readonly filteredItems = computed(() => {
    const allItems = this.items();
    const filterValue = this.filter();

    if (filterValue === 'all') return allItems;
    if (filterValue === 'low-stock') return allItems.filter(i => i.isLowStock || i.quantity === 0);
    if (filterValue === 'expiring') return allItems.filter(i => i.isExpiringSoon || (i.expirationDate && new Date(i.expirationDate) < new Date()));
    
    return allItems;
  });

  readonly unresolvedAlerts = computed(() => 
    this.alerts().filter(a => !a.isResolved)
  );

  readonly lowStockCount = computed(() => 
    this.items().filter(i => i.isLowStock).length
  );

  readonly outOfStockCount = computed(() => 
    this.items().filter(i => i.quantity === 0).length
  );

  readonly expiringCount = computed(() => 
    this.items().filter(i => i.isExpiringSoon).length
  );

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading.set(true);

    this.inventoryService.getAll().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error cargando inventario:', err);
        this.loading.set(false);
      }
    });

    this.inventoryService.getAlerts(false).subscribe({
      next: (alerts) => {
        this.alerts.set(alerts);
      },
      error: (err) => {
        console.error('Error cargando alertas:', err);
      }
    });
  }

  setFilter(value: 'all' | 'low-stock' | 'expiring') {
    this.filter.set(value);
  }

  deleteItem(id: string | number) {
    if (!confirm('¿Está seguro de eliminar este artículo del inventario?')) {
      return;
    }

    this.inventoryService.delete(id).subscribe({
      next: () => {
        this.items.set(this.items().filter(i => i.id !== id));
        this.loadData(); // Recargar para actualizar alertas
      },
      error: (err) => {
        console.error('Error eliminando item:', err);
        alert('Error al eliminar el artículo');
      }
    });
  }

  resolveAlert(alertId: string) {
    this.inventoryService.resolveAlert(alertId).subscribe({
      next: () => {
        this.alerts.set(this.alerts().map(a => 
          a.id === alertId ? { ...a, isResolved: true, resolvedAt: new Date().toISOString() } : a
        ));
      },
      error: (err) => {
        console.error('Error resolviendo alerta:', err);
      }
    });
  }

  getAlertTypeClass(type: string): string {
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

  getAlertIcon(type: string): string {
    switch (type) {
      case 'OutOfStock':
        return 'alert-circle';
      case 'LowStock':
        return 'alert-triangle';
      case 'Expired':
        return 'x-circle';
      case 'ExpirationWarning':
        return 'clock';
      default:
        return 'info';
    }
  }

  saveItem() {
    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const formValue = this.itemForm.getRawValue();
    
    const request = {
      name: formValue.name,
      description: formValue.description || undefined,
      sku: formValue.sku || undefined,
      quantity: formValue.quantity,
      minimumQuantity: formValue.minimumQuantity,
      unitPrice: formValue.unitPrice,
      supplier: formValue.supplier || undefined,
      expirationDate: (formValue.expirationDate && formValue.expirationDate.trim() !== '') 
        ? formValue.expirationDate 
        : undefined
    };

    const operation = this.editingItem()
      ? this.inventoryService.update(this.editingItem()!.id, request)
      : this.inventoryService.create(request);

    operation.subscribe({
      next: () => {
        this.closeModal();
        this.loadData();
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error guardando item:', err);
        alert('Error al guardar el artículo');
        this.loading.set(false);
      }
    });
  }

  editItem(item: InventoryItem) {
    this.editingItem.set(item);
    this.itemForm.patchValue({
      name: item.name,
      description: item.description || '',
      sku: item.sku || '',
      quantity: item.quantity,
      minimumQuantity: item.minimumQuantity,
      unitPrice: item.unitPrice,
      supplier: item.supplier || '',
      expirationDate: item.expirationDate || ''
    });
    this.showNewItemForm.set(true);
  }

  closeModal() {
    this.showNewItemForm.set(false);
    this.editingItem.set(null);
    this.itemForm.reset();
  }

  onModalBackdropClick(event: MouseEvent) {
    if (event.target === event.currentTarget) {
      this.closeModal();
    }
  }
}

