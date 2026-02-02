import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgFor, NgIf, CurrencyPipe } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';

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
    formaPago: ['efectivo', Validators.required],
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

  constructor() {
    this.agregarItem(); // Agregar primer item por defecto
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

    try {
      // Aquí iría la lógica de guardado y envío al SRI
      const facturaData = {
        ...this.facturaForm.value,
        subtotal: this.subtotal,
        iva: this.iva,
        total: this.total,
        fecha: new Date()
      };

      console.log('Factura a guardar:', facturaData);

      // Simular envío al SRI
      await new Promise(resolve => setTimeout(resolve, 2000));

      alert('Factura creada y enviada al SRI exitosamente');
      this.router.navigate(['/odontologo/facturacion']);

    } catch (error) {
      console.error('Error al guardar factura:', error);
      alert('Error al guardar la factura');
    } finally {
      this.loading.set(false);
    }
  }

  cancelar() {
    if (confirm('¿Está seguro de cancelar? Se perderán los datos ingresados.')) {
      this.router.navigate(['/odontologo/facturacion']);
    }
  }
}
