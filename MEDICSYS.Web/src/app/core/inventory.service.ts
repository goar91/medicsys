import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';
import { InventoryItem } from './models';

// Re-export para compatibilidad
export type { InventoryItem };

export interface InventoryAlert {
  id: string;
  inventoryItemId: string;
  type: 'LowStock' | 'OutOfStock' | 'ExpirationWarning' | 'Expired';
  message: string;
  isResolved: boolean;
  createdAt: string;
  resolvedAt?: string;
  inventoryItem?: InventoryItem;
}

export interface CreateInventoryItemRequest {
  name: string;
  description?: string;
  sku?: string;
  quantity: number;
  minimumQuantity: number;
  unitPrice: number;
  supplier?: string;
  expirationDate?: string;
}

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${API_BASE_URL}/odontologia/inventory`;

  getAll(): Observable<InventoryItem[]> {
    return this.http.get<InventoryItem[]>(this.apiUrl);
  }

  getById(id: string): Observable<InventoryItem> {
    return this.http.get<InventoryItem>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateInventoryItemRequest): Observable<InventoryItem> {
    return this.http.post<InventoryItem>(this.apiUrl, request);
  }

  update(id: string | number, request: CreateInventoryItemRequest): Observable<InventoryItem> {
    return this.http.put<InventoryItem>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string | number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getAlerts(resolved?: boolean): Observable<InventoryAlert[]> {
    let params = new HttpParams();
    if (resolved !== undefined) {
      params = params.set('resolved', resolved.toString());
    }
    return this.http.get<InventoryAlert[]>(`${this.apiUrl}/alerts`, { params });
  }

  resolveAlert(alertId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/alerts/${alertId}/resolve`, {});
  }

  checkAlerts(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/check-alerts`, {});
  }
}
