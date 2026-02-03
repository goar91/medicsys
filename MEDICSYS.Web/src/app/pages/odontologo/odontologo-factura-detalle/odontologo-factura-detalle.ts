import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgIf, NgFor, DatePipe, CurrencyPipe } from '@angular/common';
import { InvoiceService } from '../../../core/invoice.service';
import { Invoice } from '../../../core/models';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';

@Component({
  selector: 'app-odontologo-factura-detalle',
  standalone: true,
  imports: [NgIf, NgFor, DatePipe, CurrencyPipe, TopNavComponent],
  templateUrl: './odontologo-factura-detalle.html',
  styleUrl: './odontologo-factura-detalle.scss'
})
export class OdontologoFacturaDetalleComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly invoicesApi = inject(InvoiceService);

  readonly invoice = signal<Invoice | null>(null);
  readonly loading = signal(true);
  readonly printMode = signal(false);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    const print = this.route.snapshot.queryParamMap.get('print');
    this.printMode.set(print === '1');
    if (!id) {
      this.router.navigate(['/odontologo/facturacion']);
      return;
    }

    this.invoicesApi.getInvoice(id).subscribe({
      next: invoice => {
        this.invoice.set(invoice);
        this.loading.set(false);
        if (this.printMode()) {
          setTimeout(() => window.print(), 500);
        }
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  sendToSri() {
    const invoice = this.invoice();
    if (!invoice) return;
    this.invoicesApi.sendToSri(invoice.id).subscribe({
      next: updated => this.invoice.set(updated)
    });
  }

  print() {
    window.print();
  }

  back() {
    this.router.navigate(['/odontologo/facturacion']);
  }

  formatStatus(status?: Invoice['status']) {
    switch (status) {
      case 'Authorized':
        return 'Autorizada SRI';
      case 'Rejected':
        return 'Rechazada';
      default:
        return 'Pendiente';
    }
  }

  formatPayment(method?: string) {
    if (!method) return '';
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
}
