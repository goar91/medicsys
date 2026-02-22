import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from './api.config';
import { Invoice } from './models';

export interface InvoiceCreatePayload {
  customerIdentificationType: string;
  customerIdentification: string;
  customerName: string;
  customerAddress?: string | null;
  customerPhone?: string | null;
  customerEmail?: string | null;
  observations?: string | null;
  paymentMethod: string;
  cardType?: string | null;
  cardFeePercent?: number | null;
  cardInstallments?: number | null;
  paymentReference?: string | null;
  sriEnvironment?: 'Pruebas' | 'Produccion';
  sendToSri: boolean;
  items: Array<{
    description: string;
    quantity: number;
    unitPrice: number;
    discountPercent: number;
  }>;
}

export interface InvoiceConfig {
  establishmentCode: string;
  emissionPoint: string;
  nextSequential: number;
  nextNumber: string;
}

export interface InvoiceConfigUpdate {
  establishmentCode: string;
  emissionPoint: string;
}

export interface SendAwaitingSriResponse {
  total: number;
  authorized: number;
  rejected: number;
  awaitingAuthorization: number;
  pending: number;
  errors: number;
  results: Array<{
    id: string;
    number: string;
    status: string;
    error?: string;
  }>;
}

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private readonly baseUrl = `${API_BASE_URL}/invoices`;

  constructor(private readonly http: HttpClient) {}

  getInvoices(status?: string) {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<Invoice[]>(this.baseUrl, { params });
  }

  getAwaitingAuthorizationDocuments() {
    return this.http.get<Invoice[]>(`${this.baseUrl}/awaiting-authorization`);
  }

  getInvoice(id: string) {
    return this.http.get<Invoice>(`${this.baseUrl}/${id}`);
  }

  createInvoice(payload: InvoiceCreatePayload) {
    return this.http.post<Invoice>(this.baseUrl, payload);
  }

  sendToSri(id: string) {
    return this.http.post<Invoice>(`${this.baseUrl}/${id}/send-sri`, {});
  }

  sendAwaitingToSri() {
    return this.http.post<SendAwaitingSriResponse>(`${this.baseUrl}/send-awaiting-sri`, {});
  }

  getConfig() {
    return this.http.get<InvoiceConfig>(`${this.baseUrl}/config`);
  }

  updateConfig(data: InvoiceConfigUpdate) {
    return this.http.put<InvoiceConfig>(`${this.baseUrl}/config`, data);
  }
}
