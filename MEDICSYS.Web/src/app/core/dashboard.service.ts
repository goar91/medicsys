import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, map } from 'rxjs';
import { API_BASE_URL } from './api.config';

export interface DashboardStats {
  accounting: {
    totalIncome: number;
    totalExpense: number;
    profit: number;
    profitMargin: number;
  };
  invoices: {
    total: number;
    pending: number;
    authorized: number;
    totalAmount: number;
    pendingAmount: number;
  };
  expenses: {
    total: number;
    monthExpenses: number;
    weekExpenses: number;
    categories: Array<{ category: string; total: number }>;
  };
  inventory: {
    totalItems: number;
    lowStockItems: number;
    expiringItems: number;
    totalValue: number;
  };
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly baseUrl = API_BASE_URL;

  constructor(private readonly http: HttpClient) {}

  getDashboardStats(params?: { from?: string; to?: string }): Observable<DashboardStats> {
    const queryString = params 
      ? `?from=${params.from || ''}&to=${params.to || ''}`
      : '';

    return forkJoin({
      accounting: this.http.get<any>(`${this.baseUrl}/accounting/summary${queryString}`),
      invoices: this.http.get<any>(`${this.baseUrl}/sri/stats${queryString}`),
      expenses: this.http.get<any>(`${this.baseUrl}/odontologia/gastos/summary`),
      inventory: this.http.get<any>(`${this.baseUrl}/odontologia/kardex/items`)
    }).pipe(
      map(data => ({
        accounting: {
          totalIncome: data.accounting?.totalIncome || 0,
          totalExpense: data.accounting?.totalExpense || 0,
          profit: (data.accounting?.totalIncome || 0) - (data.accounting?.totalExpense || 0),
          profitMargin: data.accounting?.totalIncome 
            ? (((data.accounting.totalIncome - data.accounting.totalExpense) / data.accounting.totalIncome) * 100)
            : 0
        },
        invoices: {
          total: data.invoices?.total || 0,
          pending: data.invoices?.pending || 0,
          authorized: data.invoices?.authorized || 0,
          totalAmount: data.invoices?.totalAmount || 0,
          pendingAmount: data.invoices?.pendingAmount || 0
        },
        expenses: {
          total: data.expenses?.totalExpenses || 0,
          monthExpenses: data.expenses?.monthExpenses || 0,
          weekExpenses: data.expenses?.weekExpenses || 0,
          categories: data.expenses?.expensesByCategory || []
        },
        inventory: {
          totalItems: data.inventory?.length || 0,
          lowStockItems: data.inventory?.filter((i: any) => i.isLowStock).length || 0,
          expiringItems: data.inventory?.filter((i: any) => i.isExpiringSoon).length || 0,
          totalValue: data.inventory?.reduce((sum: number, i: any) => 
            sum + (i.quantity * (i.averageCost || i.unitPrice)), 0) || 0
        }
      }))
    );
  }
}
