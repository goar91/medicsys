import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';

export interface FinancialReport {
  period: { start: string; end: string };
  summary: {
    totalIncome: number;
    totalExpenses: number;
    totalPurchases: number;
    profit: number;
    profitMargin: number;
  };
  incomeByMonth: Array<{ month: string; amount: number }>;
  expensesByMonth: Array<{ month: string; amount: number }>;
  purchasesByMonth: Array<{ month: string; amount: number }>;
  expensesByCategory: Array<{ category: string; amount: number }>;
  inventorySummary: {
    totalItems: number;
    totalValue: number;
    lowStockItems: number;
    averageStock: number;
  };
}

export interface SalesReport {
  period: { start: string; end: string };
  summary: {
    totalSales: number;
    invoiceCount: number;
    averageTicket: number;
  };
  salesByDay: Array<{ date: string; count: number; amount: number }>;
  topServices: Array<{ service: string; quantity: number; revenue: number }>;
  paymentMethods: Array<{ method: string; count: number; amount: number }>;
}

export interface ComparativeReport {
  months: number;
  data: Array<{
    month: string;
    monthName: string;
    income: number;
    expenses: number;
    profit: number;
  }>;
  averageIncome: number;
  averageExpenses: number;
  averageProfit: number;
}

@Injectable({ providedIn: 'root' })
export class ReportesService {
  private readonly apiUrl = `${API_BASE_URL}/odontologia/reportes`;

  constructor(private readonly http: HttpClient) {}

  getFinancialReport(startDate?: string, endDate?: string): Observable<FinancialReport> {
    let url = `${this.apiUrl}/financiero`;
    const params = new URLSearchParams();
    
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    
    if (params.toString()) url += `?${params.toString()}`;
    
    return this.http.get<FinancialReport>(url);
  }

  getSalesReport(startDate?: string, endDate?: string): Observable<SalesReport> {
    let url = `${this.apiUrl}/ventas`;
    const params = new URLSearchParams();
    
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    
    if (params.toString()) url += `?${params.toString()}`;
    
    return this.http.get<SalesReport>(url);
  }

  getComparativeReport(months: number = 12): Observable<ComparativeReport> {
    return this.http.get<ComparativeReport>(`${this.apiUrl}/comparativo?months=${months}`);
  }
}
