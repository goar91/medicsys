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
  sendToSri: boolean;
  items: Array<{
    description: string;
    quantity: number;
    unitPrice: number;
    discountPercent: number;
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

  getInvoice(id: string) {
    return this.http.get<Invoice>(`${this.baseUrl}/${id}`);
  }

  createInvoice(payload: InvoiceCreatePayload) {
    return this.http.post<Invoice>(this.baseUrl, payload);
  }

  sendToSri(id: string) {
    return this.http.post<Invoice>(`${this.baseUrl}/${id}/send-sri`, {});
  }
}
