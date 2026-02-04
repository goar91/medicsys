import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';
import { Expense, ExpenseSummary } from './models';

@Injectable({ providedIn: 'root' })
export class GastosService {
  private readonly apiUrl = `${API_BASE_URL}/odontologia/gastos`;

  constructor(private readonly http: HttpClient) {}

  getAll(filters?: {
    startDate?: string;
    endDate?: string;
    category?: string;
    paymentMethod?: string;
  }): Observable<Expense[]> {
    let url = this.apiUrl;
    const params = new URLSearchParams();
    
    if (filters?.startDate) params.append('startDate', filters.startDate);
    if (filters?.endDate) params.append('endDate', filters.endDate);
    if (filters?.category) params.append('category', filters.category);
    if (filters?.paymentMethod) params.append('paymentMethod', filters.paymentMethod);
    
    if (params.toString()) url += `?${params.toString()}`;
    
    return this.http.get<Expense[]>(url);
  }

  getSummary(): Observable<ExpenseSummary> {
    return this.http.get<ExpenseSummary>(`${this.apiUrl}/summary`);
  }

  getById(id: string): Observable<Expense> {
    return this.http.get<Expense>(`${this.apiUrl}/${id}`);
  }

  create(expense: Partial<Expense>): Observable<Expense> {
    return this.http.post<Expense>(this.apiUrl, expense);
  }

  update(id: string, expense: Partial<Expense>): Observable<Expense> {
    return this.http.put<Expense>(`${this.apiUrl}/${id}`, expense);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
