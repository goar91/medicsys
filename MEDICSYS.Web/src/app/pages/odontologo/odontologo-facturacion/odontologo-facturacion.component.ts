import { Component, inject, signal, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgFor, NgIf, DatePipe, CurrencyPipe } from '@angular/common';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { InvoiceService } from '../../../core/invoice.service';
import { Invoice } from '../../../core/models';

@Component({
  selector: 'app-odontologo-facturacion',
  standalone: true,
  imports: [NgFor, NgIf, DatePipe, CurrencyPipe, TopNavComponent],
  templateUrl: './odontologo-facturacion.html',
  styleUrl: './odontologo-facturacion.scss'
})
export class OdontologoFacturacionComponent {
  private readonly router = inject(Router);
  private readonly invoicesApi = inject(InvoiceService);

  readonly loading = signal(false);
  readonly facturas = signal<Invoice[]>([]);
  readonly statusFilter = signal('');

  readonly filteredFacturas = computed(() => {
    const filter = this.statusFilter();
    if (!filter) return this.facturas();
    return this.facturas().filter(f => f.status === filter);
  });

  readonly totalFacturado = computed(() => {
    return this.facturas().reduce((sum, f) => sum + f.totalToCharge, 0);
  });

  readonly facturasAutorizadas = computed(() => {
    return this.facturas().filter(f => f.status === 'Authorized').length;
  });

  readonly facturasPendientes = computed(() => {
    return this.facturas().filter(f => f.status === 'Pending').length;
  });

  readonly facturasPruebas = computed(() => {
    return this.facturas().filter(f => f.sriEnvironment === 'Pruebas').length;
  });

  readonly facturasProduccion = computed(() => {
    return this.facturas().filter(f => f.sriEnvironment === 'Produccion').length;
  });

  constructor() {
    this.loadFacturas();
  }

  loadFacturas() {
    this.loading.set(true);
    this.invoicesApi.getInvoices().subscribe({
      next: items => {
        this.facturas.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  nuevaFactura() {
    this.router.navigate(['/odontologo/facturacion/new']);
  }

  verDetalle(facturaId: string) {
    this.router.navigate(['/odontologo/facturacion', facturaId]);
  }

  reenviarSRI(facturaId: string) {
    this.invoicesApi.sendToSri(facturaId).subscribe({
      next: updated => {
        this.facturas.update(items => items.map(item => item.id === updated.id ? updated : item));
      }
    });
  }

  descargarPDF(facturaId: string) {
    this.router.navigate(['/odontologo/facturacion', facturaId], { queryParams: { print: '1' } });
  }

  formatStatus(status: Invoice['status']) {
    switch (status) {
      case 'Authorized':
        return 'Autorizada SRI';
      case 'Rejected':
        return 'Rechazada';
      default:
        return 'Pendiente';
    }
  }

  formatPayment(method: string) {
    switch (method.toLowerCase()) {
      case 'card':
        return 'Tarjeta';
      case 'transfer':
        return 'Transferencia';
      case 'cash':
        return 'Efectivo';
      default:
        return method;
    }
  }

  formatSriEnvironment(environment: Invoice['sriEnvironment']) {
    return environment === 'Produccion' ? 'Producción' : 'Pruebas';
  }
}
