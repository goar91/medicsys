import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { KardexService, KardexItem, InventoryMovement, KardexReport } from '../../../core/kardex.service';

@Component({
  selector: 'app-kardex',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './kardex.component.html',
  styleUrls: ['./kardex.component.scss']
})
export class KardexComponent implements OnInit {
  private kardexService = inject(KardexService);
  private fb = inject(FormBuilder);

  items = signal<KardexItem[]>([]);
  movements = signal<InventoryMovement[]>([]);
  selectedItem = signal<KardexItem | null>(null);
  kardexReport = signal<KardexReport | null>(null);
  
  showModal = signal(false);
  modalType = signal<'entry' | 'exit' | 'adjustment' | 'edit' | 'kardex' | 'create'>('entry');
  
  loading = signal(false);
  movementForm!: FormGroup;
  editForm!: FormGroup;
  createForm!: FormGroup;

  // Filters
  filterText = signal<string>('');
  filterLowStock = signal<boolean>(false);
  filterExpiring = signal<boolean>(false);

  filteredItems = computed(() => {
    let filtered = this.items();

    if (this.filterText()) {
      const text = this.filterText().toLowerCase();
      filtered = filtered.filter(i => 
        i.name.toLowerCase().includes(text) ||
        i.sku?.toLowerCase().includes(text) ||
        i.description?.toLowerCase().includes(text)
      );
    }

    if (this.filterLowStock()) {
      filtered = filtered.filter(i => i.isLowStock);
    }

    if (this.filterExpiring()) {
      filtered = filtered.filter(i => i.isExpiringSoon);
    }

    return filtered;
  });

  ngOnInit() {
    this.initForms();
    this.loadItems();
  }

  initForms() {
    this.movementForm = this.fb.group({
      inventoryItemId: ['', Validators.required],
      quantity: [0, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      newQuantity: [0],
      reason: [''],
      movementDate: [new Date().toISOString().split('T')[0]],
      reference: [''],
      notes: ['']
    });

    this.editForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      sku: [''],
      minimumQuantity: [0, Validators.min(0)],
      maximumQuantity: [null],
      reorderPoint: [null],
      unitPrice: [0, Validators.min(0)],
      supplier: [''],
      location: [''],
      batch: [''],
      expirationDate: [null]
    });
    this.createForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      sku: [''],
      initialQuantity: [0, Validators.min(0)],
      minimumQuantity: [0, Validators.min(0)],
      maximumQuantity: [null],
      reorderPoint: [null],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
      supplier: [''],
      location: [''],
      batch: [''],
      expirationDate: [null]
    });  }

  loadItems() {
    this.loading.set(true);
    this.kardexService.getItems().subscribe({
      next: (data) => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading items:', err);
        this.loading.set(false);
      }
    });
  }

  openModal(type: 'entry' | 'exit' | 'adjustment' | 'edit' | 'kardex' | 'create', item?: KardexItem) {
    this.modalType.set(type);
    this.selectedItem.set(item || null);

    if (item) {
      if (type === 'edit') {
        this.editForm.patchValue({
          name: item.name,
          description: item.description,
          sku: item.sku,
          minimumQuantity: item.minimumQuantity,
          maximumQuantity: item.maximumQuantity,
          reorderPoint: item.reorderPoint,
          unitPrice: item.unitPrice,
          supplier: item.supplier,
          location: item.location,
          batch: item.batch,
          expirationDate: item.expirationDate ? new Date(item.expirationDate).toISOString().split('T')[0] : null
        });
      } else if (type === 'kardex') {
        this.loadKardex(item.id);
      } else {
        this.movementForm.patchValue({
          inventoryItemId: item.id,
          unitPrice: item.averageCost || item.unitPrice
        });
      }
    } else {
      // Si no hay item seleccionado, resetear el formulario
      this.movementForm.patchValue({
        inventoryItemId: '',
        quantity: null,
        unitPrice: 0,
        newQuantity: null,
        reason: ''
      });
    }

    this.showModal.set(true);
  }

  onItemSelected(event: Event) {
    const select = event.target as HTMLSelectElement;
    const itemId = select.value;
    if (itemId) {
      const item = this.items().find(i => i.id === itemId);
      if (item) {
        this.selectedItem.set(item);
        this.movementForm.patchValue({
          unitPrice: item.averageCost || item.unitPrice
        });
      }
    }
  }

  closeModal() {
    this.showModal.set(false);
    this.movementForm.reset({ movementDate: new Date().toISOString().split('T')[0] });
    this.editForm.reset();
    this.createForm.reset({ initialQuantity: 0, minimumQuantity: 0, unitPrice: 0 });
    this.kardexReport.set(null);
    this.selectedItem.set(null);
  }

  saveMovement() {
    if (this.movementForm.invalid) return;

    const formValue = this.movementForm.value;
    const type = this.modalType();

    let request: any;
    if (type === 'entry' || type === 'exit') {
      request = this.kardexService[type === 'entry' ? 'addEntry' : 'addExit']({
        inventoryItemId: formValue.inventoryItemId,
        quantity: formValue.quantity,
        unitPrice: formValue.unitPrice,
        movementDate: formValue.movementDate,
        reference: formValue.reference,
        notes: formValue.notes
      });
    } else if (type === 'adjustment') {
      request = this.kardexService.addAdjustment({
        inventoryItemId: formValue.inventoryItemId,
        newQuantity: formValue.newQuantity,
        reason: formValue.reason,
        movementDate: formValue.movementDate,
        notes: formValue.notes
      });
    }

    request?.subscribe({
      next: () => {
        this.closeModal();
        this.loadItems();
      },
      error: (err: any) => console.error('Error saving movement:', err)
    });
  }

  saveEdit() {
    if (this.editForm.invalid || !this.selectedItem()) return;

    const formValue = this.editForm.value;
    this.kardexService.updateItem(this.selectedItem()!.id, formValue).subscribe({
      next: () => {
        this.closeModal();
        this.loadItems();
      },
      error: (err) => console.error('Error updating item:', err)
    });
  }
saveCreate() {
    if (this.createForm.invalid) return;

    const formValue = this.createForm.value;
    this.kardexService.createItem(formValue).subscribe({
      next: () => {
        this.closeModal();
        this.loadItems();
      },
      error: (err: any) => console.error('Error creating item:', err)
    });
  }

  
  loadKardex(itemId: string) {
    this.kardexService.getKardex(itemId).subscribe({
      next: (data) => this.kardexReport.set(data),
      error: (err) => console.error('Error loading kardex:', err)
    });
  }

  getStatusClass(item: KardexItem): string {
    if (item.isLowStock) return 'low-stock';
    if (item.needsReorder) return 'reorder';
    if (item.isExpiringSoon) return 'expiring';
    return 'normal';
  }

  getMovementTypeLabel(type: string): string {
    const labels: Record<string, string> = {
      'Entry': 'Entrada',
      'Exit': 'Salida',
      'Adjustment': 'Ajuste'
    };
    return labels[type] || type;
  }
}
