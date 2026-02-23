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
  private static readonly LetterPrintableHeightPx = 1020;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly invoicesApi = inject(InvoiceService);

  readonly invoice = signal<Invoice | null>(null);
  readonly loading = signal(true);
  readonly printMode = signal(false);
  readonly loadError = signal(false);
  readonly printScale = signal(1);

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
        this.loadError.set(false);
        this.loading.set(false);
        if (this.printMode()) {
          this.print();
        }
      },
      error: () => {
        this.loadError.set(true);
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
    this.triggerPrintWhenReady();
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
      case 'AwaitingAuthorization':
        return 'En espera de autorización';
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

  /**
   * Divide la clave de acceso de 49 dígitos en grupos de 7 para mejor legibilidad
   */
  splitAccessKey(accessKey: string | null | undefined): string[] {
    if (!accessKey) return [];
    const groups: string[] = [];
    for (let i = 0; i < accessKey.length; i += 7) {
      groups.push(accessKey.slice(i, i + 7));
    }
    return groups;
  }

  /**
   * Genera segmentos para el código de barras visual basado en la clave de acceso
   */
  getAccessKeySegments(accessKey: string | null | undefined): { width: number; opacity: number }[] {
    if (!accessKey) return [];
    const segments: { width: number; opacity: number }[] = [];

    for (let i = 0; i < accessKey.length; i++) {
      const digit = parseInt(accessKey[i], 10) || 0;
      // Ancho basado en el dígito (2-4px)
      const width = 2 + (digit % 3);
      // Opacidad alterna para crear patrón visual
      const opacity = digit % 2 === 0 ? 1 : 0.7;
      segments.push({ width, opacity });

      // Agregar espacio entre dígitos
      if (i < accessKey.length - 1) {
        segments.push({ width: 1, opacity: 0 });
      }
    }

    return segments;
  }

  formatAccessKeyCompact(accessKey: string | null | undefined): string {
    if (!accessKey) {
      return '';
    }

    const normalized = accessKey.replace(/\s+/g, '');
    if (normalized.length <= 32) {
      return normalized;
    }

    return `${normalized.slice(0, 16)}...${normalized.slice(-16)}`;
  }

  private triggerPrintWhenReady(maxAttempts = 30) {
    let attempts = 0;

    const tryPrint = () => {
      attempts++;
      const hasInvoice = !!this.invoice();
      const printNode = document.querySelector('.print-clean-sheet');

      if ((!hasInvoice || !printNode) && attempts < maxAttempts) {
        setTimeout(tryPrint, 120);
        return;
      }

      this.applyPrintScale();

      // Espera dos frames para garantizar layout/paint antes de abrir el diálogo.
      requestAnimationFrame(() => {
        requestAnimationFrame(() => {
          window.print();
          this.resetPrintScale();
        });
      });
    };

    tryPrint();
  }

  private applyPrintScale() {
    const sheet = document.querySelector<HTMLElement>('.print-clean-sheet');
    if (!sheet) {
      return;
    }

    this.printScale.set(1);
    document.documentElement.style.setProperty('--invoice-print-scale', '1');

    // Forzamos un layout pass con escala 1 para medir altura real.
    void sheet.offsetHeight;

    const realHeight = sheet.scrollHeight;
    const available = OdontologoFacturaDetalleComponent.LetterPrintableHeightPx;
    const scale = realHeight > available
      ? Math.max(0.55, available / realHeight)
      : 1;

    const finalScale = Number(scale.toFixed(3));
    this.printScale.set(finalScale);
    document.documentElement.style.setProperty('--invoice-print-scale', `${finalScale}`);
  }

  private resetPrintScale() {
    this.printScale.set(1);
    document.documentElement.style.setProperty('--invoice-print-scale', '1');
  }
}
