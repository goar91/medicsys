import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgFor, NgIf, CurrencyPipe } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { InvoiceService } from '../../../core/invoice.service';

interface FacturaItem {
  cantidad: number;
  descripcion: string;
  precioUnitario: number;
  descuento: number;
  subtotal: number;
}

interface Cliente {
  tipoIdentificacion: string;
  identificacion: string;
  nombre: string;
  direccion: string;
  telefono: string;
  email: string;
}

@Component({
  selector: 'app-odontologo-factura-form',
  standalone: true,
  imports: [NgFor, NgIf, CurrencyPipe, ReactiveFormsModule, RouterLink, TopNavComponent],
  templateUrl: './odontologo-factura-form.html',
  styleUrl: './odontologo-factura-form.scss'
})
export class OdontologoFacturaFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly invoicesApi = inject(InvoiceService);

  readonly loading = signal(false);
  readonly showClienteForm = signal(false);
  readonly esConsumidorFinal = signal(false);

  readonly facturaForm: FormGroup = this.fb.group({
    cliente: this.fb.group({
      tipoIdentificacion: ['04', Validators.required], // RUC
      identificacion: ['', Validators.required],
      nombre: ['', Validators.required],
      direccion: [''],
      telefono: [''],
      email: ['', [Validators.email]]
    }),
    items: this.fb.array([]),
    formaPago: ['Cash', Validators.required],
    cardType: [''],
    cardFeePercent: [null],
    cardInstallments: [null],
    paymentReference: [''],
    sendToSri: [true],
    observaciones: ['']
  });

  // Clientes frecuentes
  readonly clientesFrecuentes = signal([
    {
      identificacion: '0102345678',
      nombre: 'María González',
      email: 'maria@email.com',
      telefono: '0987654321'
    },
    {
      identificacion: '0103456789',
      nombre: 'Juan Pérez',
      email: 'juan@email.com',
      telefono: '0987654322'
    },
    {
      identificacion: '0104567890',
      nombre: 'Ana Rodríguez',
      email: 'ana@email.com',
      telefono: '0987654323'
    }
  ]);

  // Servicios predefinidos
  readonly serviciosPredefinidos = signal([
    { descripcion: 'Consulta General', precio: 35.00 },
    { descripcion: 'Limpieza Dental', precio: 45.00 },
    { descripcion: 'Extracción Simple', precio: 50.00 },
    { descripcion: 'Extracción Compleja', precio: 85.00 },
    { descripcion: 'Resina Dental', precio: 65.00 },
    { descripcion: 'Endodoncia', precio: 150.00 },
    { descripcion: 'Corona', precio: 320.00 },
    { descripcion: 'Implante Dental', precio: 850.00 },
    { descripcion: 'Ortodoncia - Mensualidad', precio: 120.00 },
    { descripcion: 'Blanqueamiento Dental', precio: 180.00 }
  ]);

  readonly cardFees = signal([
    { type: 'Débito', percent: 1.5 },
    { type: 'Crédito', percent: 3.5 },
    { type: 'American Express', percent: 5.0 },
    { type: 'Diners', percent: 6.0 }
  ]);

  get items(): FormArray {
    return this.facturaForm.get('items') as FormArray;
  }

  get cliente(): FormGroup {
    return this.facturaForm.get('cliente') as FormGroup;
  }

  get subtotal(): number {
    return this.items.controls.reduce((sum, item) => {
      const cantidad = item.get('cantidad')?.value || 0;
      const precio = item.get('precioUnitario')?.value || 0;
      const descuento = item.get('descuento')?.value || 0;
      return sum + (cantidad * precio * (1 - descuento / 100));
    }, 0);
  }

  get iva(): number {
    return this.subtotal * 0.15; // IVA 15% Ecuador
  }

  get total(): number {
    return this.subtotal + this.iva;
  }

  get cardFeeAmount(): number {
    if (this.facturaForm.get('formaPago')?.value !== 'Card') {
      return 0;
    }
    const percent = Number(this.facturaForm.get('cardFeePercent')?.value || 0);
    return this.total * (percent / 100);
  }

  get totalToCharge(): number {
    return this.total + this.cardFeeAmount;
  }

  constructor() {
    this.agregarItem(); // Agregar primer item por defecto
    this.facturaForm.get('formaPago')?.valueChanges.subscribe(value => {
      if (value !== 'Card') {
        this.facturaForm.patchValue({
          cardType: '',
          cardFeePercent: null,
          cardInstallments: null,
          paymentReference: ''
        });
      }
    });
  }

  seleccionarConsumidorFinal() {
    this.esConsumidorFinal.set(true);
    this.cliente.patchValue({
      tipoIdentificacion: '07', // Consumidor Final
      identificacion: '9999999999',
      nombre: 'CONSUMIDOR FINAL',
      direccion: '',
      telefono: '',
      email: ''
    });
    this.showClienteForm.set(false);
  }

  seleccionarClienteFrecuente(clienteData: any) {
    this.esConsumidorFinal.set(false);
    this.cliente.patchValue({
      tipoIdentificacion: '05', // Cédula
      identificacion: clienteData.identificacion,
      nombre: clienteData.nombre,
      direccion: '',
      telefono: clienteData.telefono,
      email: clienteData.email
    });
    this.showClienteForm.set(false);
  }

  nuevoCliente() {
    this.esConsumidorFinal.set(false);
    this.cliente.reset({
      tipoIdentificacion: '05' // Cédula por defecto
    });
    this.showClienteForm.set(true);
  }

  agregarItem() {
    const item = this.fb.group({
      cantidad: [1, [Validators.required, Validators.min(1)]],
      descripcion: ['', Validators.required],
      precioUnitario: [0, [Validators.required, Validators.min(0)]],
      descuento: [0, [Validators.min(0), Validators.max(100)]],
      subtotal: [0]
    });

    // Calcular subtotal automáticamente
    item.valueChanges.subscribe(() => {
      const cantidad = item.get('cantidad')?.value || 0;
      const precio = item.get('precioUnitario')?.value || 0;
      const descuento = item.get('descuento')?.value || 0;
      const subtotal = cantidad * precio * (1 - descuento / 100);
      item.patchValue({ subtotal }, { emitEvent: false });
    });

    this.items.push(item);
  }

  eliminarItem(index: number) {
    if (this.items.length > 1) {
      this.items.removeAt(index);
    }
  }

  agregarServicioPredefinido(servicio: any, index: number) {
    this.items.at(index).patchValue({
      descripcion: servicio.descripcion,
      precioUnitario: servicio.precio
    });
  }

  async guardarFactura() {
    if (this.facturaForm.invalid) {
      this.facturaForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const cliente = this.cliente.getRawValue();
    const payload = {
      customerIdentificationType: cliente.tipoIdentificacion,
      customerIdentification: cliente.identificacion,
      customerName: cliente.nombre,
      customerAddress: cliente.direccion,
      customerPhone: cliente.telefono,
      customerEmail: cliente.email,
      observations: this.facturaForm.get('observaciones')?.value ?? '',
      paymentMethod: this.facturaForm.get('formaPago')?.value,
      cardType: this.facturaForm.get('cardType')?.value || null,
      cardFeePercent: this.facturaForm.get('cardFeePercent')?.value ?? null,
      cardInstallments: this.facturaForm.get('cardInstallments')?.value ?? null,
      paymentReference: this.facturaForm.get('paymentReference')?.value ?? null,
      sendToSri: this.facturaForm.get('sendToSri')?.value ?? true,
      items: this.items.controls.map(item => ({
        description: item.get('descripcion')?.value,
        quantity: Number(item.get('cantidad')?.value || 0),
        unitPrice: Number(item.get('precioUnitario')?.value || 0),
        discountPercent: Number(item.get('descuento')?.value || 0)
      }))
    };

    this.invoicesApi.createInvoice(payload).subscribe({
      next: invoice => {
        const message = invoice.status === 'Authorized'
          ? 'Factura creada y autorizada por el SRI.'
          : 'Factura creada y enviada al SRI.';
        alert(message);
        this.router.navigate(['/odontologo/facturacion', invoice.id]);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        alert('Error al guardar la factura');
      }
    });
  }

  cancelar() {
    if (confirm('¿Está seguro de cancelar? Se perderán los datos ingresados.')) {
      this.router.navigate(['/odontologo/facturacion']);
    }
  }

  seleccionarTarifaTarjeta(option: { type: string; percent: number }) {
    this.facturaForm.patchValue({
      formaPago: 'Card',
      cardType: option.type,
      cardFeePercent: option.percent
    });
  }
}
