import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, NgFor, NgIf } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { TopNavComponent } from '../../../../shared/top-nav/top-nav';
import { InvoiceService, SendAwaitingSriResponse } from '../../../../core/invoice.service';
import { Invoice } from '../../../../core/models';

@Component({
  selector: 'app-documentos-espera-autorizacion',
  standalone: true,
  imports: [NgFor, NgIf, DatePipe, CurrencyPipe, RouterLink, TopNavComponent],
  templateUrl: './documentos-espera-autorizacion.component.html',
  styleUrl: './documentos-espera-autorizacion.component.scss'
})
export class DocumentosEsperaAutorizacionComponent {
  private readonly invoicesApi = inject(InvoiceService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly sendingAll = signal(false);
  readonly sendingIds = signal<Set<string>>(new Set());
  readonly summaryMessage = signal<string>('');
  readonly documentos = signal<Invoice[]>([]);

  readonly totalDocumentos = computed(() => this.documentos().length);
  readonly totalMonto = computed(() => this.documentos().reduce((sum, item) => sum + item.totalToCharge, 0));

  constructor() {
    this.loadDocumentos();
  }

  loadDocumentos() {
    this.loading.set(true);
    this.summaryMessage.set('');
    this.invoicesApi.getAwaitingAuthorizationDocuments().subscribe({
      next: documents => {
        this.documentos.set(documents);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.summaryMessage.set('No se pudieron cargar los documentos en espera.');
      }
    });
  }

  reenviarDocumento(documento: Invoice) {
    this.setSending(documento.id, true);
    this.summaryMessage.set('');

    this.invoicesApi.sendToSri(documento.id).subscribe({
      next: updated => {
        if (updated.status === 'AwaitingAuthorization') {
          this.documentos.update(items =>
            items.map(item => item.id === updated.id ? updated : item)
          );
        } else {
          this.documentos.update(items => items.filter(item => item.id !== updated.id));
        }

        this.summaryMessage.set(`Documento ${updated.number} procesado: ${this.formatStatus(updated.status)}.`);
        this.setSending(documento.id, false);
      },
      error: () => {
        this.summaryMessage.set(`No se pudo reenviar el documento ${documento.number}.`);
        this.setSending(documento.id, false);
      }
    });
  }

  reenviarTodos() {
    if (this.totalDocumentos() === 0 || this.sendingAll()) {
      return;
    }

    this.sendingAll.set(true);
    this.summaryMessage.set('');

    this.invoicesApi.sendAwaitingToSri().subscribe({
      next: response => {
        this.summaryMessage.set(this.buildBatchMessage(response));
        this.sendingAll.set(false);
        this.loadDocumentos();
      },
      error: () => {
        this.sendingAll.set(false);
        this.summaryMessage.set('No se pudieron reenviar todos los documentos.');
      }
    });
  }

  verDetalle(documentoId: string) {
    this.router.navigate(['/odontologo/facturacion', documentoId]);
  }

  isSending(documentoId: string) {
    return this.sendingIds().has(documentoId);
  }

  formatStatus(status: Invoice['status']) {
    switch (status) {
      case 'Authorized':
        return 'Autorizada';
      case 'Rejected':
        return 'Rechazada';
      case 'AwaitingAuthorization':
        return 'En espera de autorización';
      default:
        return 'Pendiente';
    }
  }

  formatSriEnvironment(environment: Invoice['sriEnvironment']) {
    return environment === 'Produccion' ? 'Producción' : 'Pruebas';
  }

  private setSending(documentoId: string, sending: boolean) {
    this.sendingIds.update(current => {
      const next = new Set(current);
      if (sending) {
        next.add(documentoId);
      } else {
        next.delete(documentoId);
      }
      return next;
    });
  }

  private buildBatchMessage(response: SendAwaitingSriResponse): string {
    if (response.total === 0) {
      return 'No hay documentos en espera para reenviar.';
    }

    return `Reenvío completado. Total: ${response.total}, autorizados: ${response.authorized}, rechazados: ${response.rejected}, aún en espera: ${response.awaitingAuthorization}, errores: ${response.errors}.`;
  }
}
