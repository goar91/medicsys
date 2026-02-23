import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgFor, NgIf, CurrencyPipe } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { InvoiceService, InvoiceConfig } from '../../../core/invoice.service';
import { Invoice } from '../../../core/models';

type SriEnvironment = 'Pruebas' | 'Produccion';

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

interface ServicioSugerido {
  descripcion: string;
  precio: number;
}

interface CardFeeOption {
  type: string;
  percent: number;
}

@Component({
  selector: 'app-odontologo-factura-form',
  standalone: true,
  imports: [NgFor, NgIf, CurrencyPipe, ReactiveFormsModule, RouterLink, TopNavComponent],
  templateUrl: './odontologo-factura-form.html',
  styleUrl: './odontologo-factura-form.scss'
})
export class OdontologoFacturaFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly invoicesApi = inject(InvoiceService);

  readonly loading = signal(false);
  readonly showClienteForm = signal(false);
  readonly esConsumidorFinal = signal(false);
  readonly editingConfig = signal(false);
  readonly savingConfig = signal(false);
  readonly invoiceConfig = signal<InvoiceConfig | null>(null);
  readonly sriEnvironments = signal<Array<{ value: SriEnvironment; title: string; description: string }>>([
    {
      value: 'Pruebas',
      title: 'Pruebas SRI',
      description: 'Ideal para validaciones internas y entrenamiento.'
    },
    {
      value: 'Produccion',
      title: 'Producción SRI',
      description: 'Usa este ambiente para facturación oficial.'
    }
  ]);

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
    sriEnvironment: ['Pruebas', Validators.required],
    sendToSri: [true],
    observaciones: ['']
  });

  readonly clientesFrecuentes = signal<Cliente[]>([]);
  readonly serviciosPredefinidos = signal<ServicioSugerido[]>([]);
  readonly cardFees = signal<CardFeeOption[]>([]);

  readonly configForm: FormGroup = this.fb.group({
    establishmentCode: ['001', [Validators.required, Validators.pattern(/^\d{1,3}$/)]],
    emissionPoint: ['002', [Validators.required, Validators.pattern(/^\d{1,3}$/)]]
  });

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

  ngOnInit() {
    this.loadConfig();
    this.loadSuggestionsFromHistory();
  }

  loadConfig() {
    this.invoicesApi.getConfig().subscribe({
      next: config => {
        this.invoiceConfig.set(config);
        this.configForm.patchValue({
          establishmentCode: config.establishmentCode,
          emissionPoint: config.emissionPoint
        });
      },
      error: () => {
        this.invoiceConfig.set(null);
        alert('No se pudo cargar la configuración de facturación desde la base de datos.');
      }
    });
  }

  loadSuggestionsFromHistory() {
    this.invoicesApi.getInvoices().subscribe({
      next: invoices => {
        this.clientesFrecuentes.set(this.buildFrequentCustomers(invoices));
        this.serviciosPredefinidos.set(this.buildSuggestedServices(invoices));
        this.cardFees.set(this.buildCardFeeOptions(invoices));
      },
      error: () => {
        this.clientesFrecuentes.set([]);
        this.serviciosPredefinidos.set([]);
        this.cardFees.set([]);
      }
    });
  }

  toggleEditConfig() {
    this.editingConfig.set(!this.editingConfig());
  }

  guardarConfig() {
    if (this.configForm.invalid) {
      this.configForm.markAllAsTouched();
      return;
    }
    this.savingConfig.set(true);
    const data = this.configForm.getRawValue();
    this.invoicesApi.updateConfig({
      establishmentCode: data.establishmentCode,
      emissionPoint: data.emissionPoint
    }).subscribe({
      next: config => {
        this.invoiceConfig.set(config);
        this.editingConfig.set(false);
        this.savingConfig.set(false);
      },
      error: () => {
        this.savingConfig.set(false);
        alert('Error al guardar la configuración');
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

  seleccionarClienteFrecuente(clienteData: Cliente) {
    this.esConsumidorFinal.set(false);
    this.cliente.patchValue({
      tipoIdentificacion: clienteData.tipoIdentificacion || '05',
      identificacion: clienteData.identificacion,
      nombre: clienteData.nombre,
      direccion: clienteData.direccion || '',
      telefono: clienteData.telefono || '',
      email: clienteData.email || ''
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

  agregarServicioPredefinido(servicio: ServicioSugerido, index: number) {
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
      sriEnvironment: (this.facturaForm.get('sriEnvironment')?.value ?? 'Pruebas') as SriEnvironment,
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
        const message = payload.sendToSri
          ? (invoice.status === 'Authorized'
              ? `Factura creada y autorizada por el SRI (${invoice.sriEnvironment}).`
              : invoice.status === 'AwaitingAuthorization'
                ? `Factura enviada al SRI (${invoice.sriEnvironment}) y movida a "Documentos en Espera de Autorización".`
                : `Factura creada y enviada al SRI (${invoice.sriEnvironment}).`)
          : `Factura creada en ambiente ${invoice.sriEnvironment}, pendiente de envío al SRI.`;
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

  seleccionarAmbiente(value: SriEnvironment) {
    this.facturaForm.patchValue({ sriEnvironment: value });
  }

  private buildFrequentCustomers(invoices: Invoice[]): Cliente[] {
    const grouped = new Map<string, { customer: Invoice['customer']; count: number }>();

    for (const invoice of invoices) {
      const customer = invoice.customer;
      if (!customer?.identification || customer.identification === '9999999999') {
        continue;
      }

      const key = customer.identification.trim();
      if (!key) {
        continue;
      }

      const existing = grouped.get(key);
      if (existing) {
        existing.count += 1;
      } else {
        grouped.set(key, {
          customer,
          count: 1
        });
      }
    }

    return Array.from(grouped.values())
      .sort((a, b) => b.count - a.count || a.customer.name.localeCompare(b.customer.name))
      .slice(0, 12)
      .map(entry => ({
        tipoIdentificacion: entry.customer.identificationType || '05',
        identificacion: entry.customer.identification,
        nombre: entry.customer.name,
        direccion: entry.customer.address || '',
        telefono: entry.customer.phone || '',
        email: entry.customer.email || ''
      }));
  }

  private buildSuggestedServices(invoices: Invoice[]): ServicioSugerido[] {
    const grouped = new Map<string, { description: string; quantity: number; revenue: number; uses: number }>();

    for (const invoice of invoices) {
      for (const item of invoice.items || []) {
        const description = item.description?.trim();
        if (!description) {
          continue;
        }

        const key = description.toLowerCase();
        const quantity = Number(item.quantity || 0);
        const revenue = Number(item.total ?? (item.unitPrice * item.quantity));
        const existing = grouped.get(key);

        if (existing) {
          existing.quantity += quantity;
          existing.revenue += revenue;
          existing.uses += 1;
        } else {
          grouped.set(key, {
            description,
            quantity,
            revenue,
            uses: 1
          });
        }
      }
    }

    return Array.from(grouped.values())
      .sort((a, b) => b.uses - a.uses || b.revenue - a.revenue)
      .slice(0, 12)
      .map(item => ({
        descripcion: item.description,
        precio: Number((item.revenue / Math.max(item.quantity, 1)).toFixed(2))
      }));
  }

  private buildCardFeeOptions(invoices: Invoice[]): CardFeeOption[] {
    const grouped = new Map<string, { count: number; percentSum: number }>();

    for (const invoice of invoices) {
      if (invoice.paymentMethod !== 'Card' || invoice.cardFeePercent == null || invoice.cardFeePercent <= 0) {
        continue;
      }

      const cardType = invoice.cardType?.trim() || 'Tarjeta';
      const existing = grouped.get(cardType);
      if (existing) {
        existing.count += 1;
        existing.percentSum += invoice.cardFeePercent;
      } else {
        grouped.set(cardType, { count: 1, percentSum: invoice.cardFeePercent });
      }
    }

    return Array.from(grouped.entries())
      .sort((a, b) => b[1].count - a[1].count)
      .slice(0, 8)
      .map(([type, stats]) => ({
        type,
        percent: Number((stats.percentSum / stats.count).toFixed(2))
      }));
  }
}
