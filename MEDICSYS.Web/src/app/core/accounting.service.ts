import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from './api.config';
import { AccountingCategory, AccountingEntry, AccountingSummary } from './models';

export interface AccountingEntryPayload {
  date: string;
  categoryId: string;
  description: string;
  amount: number;
  type: 'Income' | 'Expense';
  paymentMethod?: string | null;
  reference?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AccountingService {
  private readonly baseUrl = `${API_BASE_URL}/accounting`;

  constructor(private readonly http: HttpClient) {}

  getCategories() {
    return this.http.get<AccountingCategory[]>(`${this.baseUrl}/categories`);
  }

  getEntries(params?: { from?: string; to?: string; type?: string; categoryId?: string }) {
    let httpParams = new HttpParams();
    if (params?.from) httpParams = httpParams.set('from', params.from);
    if (params?.to) httpParams = httpParams.set('to', params.to);
    if (params?.type) httpParams = httpParams.set('type', params.type);
    if (params?.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    return this.http.get<AccountingEntry[]>(`${this.baseUrl}/entries`, { params: httpParams });
  }

  createEntry(payload: AccountingEntryPayload) {
    return this.http.post<AccountingEntry>(`${this.baseUrl}/entries`, payload);
  }

  getSummary(params?: { from?: string; to?: string }) {
    let httpParams = new HttpParams();
    if (params?.from) httpParams = httpParams.set('from', params.from);
    if (params?.to) httpParams = httpParams.set('to', params.to);
    return this.http.get<AccountingSummary>(`${this.baseUrl}/summary`, { params: httpParams });
  }
}
