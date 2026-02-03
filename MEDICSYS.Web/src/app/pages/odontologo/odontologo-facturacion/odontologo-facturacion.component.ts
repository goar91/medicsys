import { Component, inject, signal, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgFor, NgIf, DatePipe, CurrencyPipe } from '@angular/common';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';

declare const ngDevMode: boolean;

interface Factura {
  id: string;
  numero: string;
  fecha: Date;
  cliente: {
    identificacion: string;
    nombre: string;
    email: string;
  };
  subtotal: number;
  iva: number;
  total: number;
  estado: 'pendiente' | 'autorizada' | 'rechazada';
  autorizacionSRI?: string;
  formaPago: string;
}

@Component({
  selector: 'app-odontologo-facturacion',
  standalone: true,
  imports: [NgFor, NgIf, DatePipe, CurrencyPipe, TopNavComponent],
  templateUrl: './odontologo-facturacion.html',
  styleUrl: './odontologo-facturacion.scss'
})
export class OdontologoFacturacionComponent {
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly facturas = signal<Factura[]>([
    {
      id: '1',
      numero: '001-001-000000001',
      fecha: new Date('2026-02-01'),
      cliente: {
        identificacion: '0102345678',
        nombre: 'María González',
        email: 'maria@email.com'
      },
      subtotal: 150.00,
      iva: 18.00,
      total: 168.00,
      estado: 'autorizada',
      autorizacionSRI: '0102202601123456789012345678901234567890',
      formaPago: 'Tarjeta'
    },
    {
      id: '2',
      numero: '001-001-000000002',
      fecha: new Date('2026-01-31'),
      cliente: {
        identificacion: '9999999999',
        nombre: 'Consumidor Final',
        email: ''
      },
      subtotal: 75.00,
      iva: 9.00,
      total: 84.00,
      estado: 'autorizada',
      autorizacionSRI: '0102202601123456789012345678901234567891',
      formaPago: 'Efectivo'
    },
    {
      id: '3',
      numero: '001-001-000000003',
      fecha: new Date('2026-01-30'),
      cliente: {
        identificacion: '0103456789',
        nombre: 'Juan Pérez',
        email: 'juan@email.com'
      },
      subtotal: 320.00,
      iva: 38.40,
      total: 358.40,
      estado: 'pendiente',
      formaPago: 'Transferencia'
    }
  ]);

  readonly totalFacturado = computed(() => {
    return this.facturas().reduce((sum, f) => sum + f.total, 0);
  });

  readonly facturasAutorizadas = computed(() => {
    return this.facturas().filter(f => f.estado === 'autorizada').length;
  });

  readonly facturasPendientes = computed(() => {
    return this.facturas().filter(f => f.estado === 'pendiente').length;
  });

  nuevaFactura() {
    this.router.navigate(['/odontologo/facturacion/new']);
  }

  verDetalle(facturaId: string) {
    if (typeof ngDevMode !== 'undefined' && ngDevMode) {
      console.debug('[DEV] Ver detalle factura:', facturaId);
    }
  }

  reenviarSRI(facturaId: string) {
    if (typeof ngDevMode !== 'undefined' && ngDevMode) {
      console.debug('[DEV] Reenviar al SRI:', facturaId);
    }
  }

  descargarPDF(facturaId: string) {
    if (typeof ngDevMode !== 'undefined' && ngDevMode) {
      console.debug('[DEV] Descargar PDF:', facturaId);
    }
  }
}
