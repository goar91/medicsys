import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';

export interface KardexItem {
  id: string;
  name: string;
  description?: string;
  sku?: string;
  quantity: number;
  minimumQuantity: number;
  maximumQuantity?: number;
  reorderPoint?: number;
  unitPrice: number;
  averageCost?: number;
  supplier?: string;
  location?: string;
  batch?: string;
  expirationDate?: string;
  isLowStock: boolean;
  isExpiringSoon: boolean;
  needsReorder: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface InventoryMovement {
  id: string;
  inventoryItemId: string;
  inventoryItemName: string;
  movementDate: string;
  movementType: 'Entry' | 'Exit' | 'Adjustment';
  quantity: number;
  unitPrice: number;
  totalCost: number;
  stockBefore: number;
  stockAfter: number;
  reference?: string;
  notes?: string;
  createdAt: string;
}

export interface KardexReport {
  item: KardexItem;
  movements: InventoryMovement[];
  summary: {
    totalEntries: number;
    totalExits: number;
    totalAdjustments: number;
    currentStock: number;
    averageCost: number;
  };
}

@Injectable({ providedIn: 'root' })
export class KardexService {
  private readonly apiUrl = `${API_BASE_URL}/odontologia/kardex`;

  constructor(private readonly http: HttpClient) {}

  getItems(): Observable<KardexItem[]> {
    return this.http.get<KardexItem[]>(`${this.apiUrl}/items`);
  }

  getItem(id: string): Observable<KardexItem> {
    return this.http.get<KardexItem>(`${this.apiUrl}/items/${id}`);
  }

  getMovements(filters?: {
    inventoryItemId?: string;
    startDate?: string;
    endDate?: string;
    movementType?: string;
  }): Observable<InventoryMovement[]> {
    let url = `${this.apiUrl}/movements`;
    const params = new URLSearchParams();
    
    if (filters?.inventoryItemId) params.append('inventoryItemId', filters.inventoryItemId);
    if (filters?.startDate) params.append('startDate', filters.startDate);
    if (filters?.endDate) params.append('endDate', filters.endDate);
    if (filters?.movementType) params.append('movementType', filters.movementType);
    
    if (params.toString()) url += `?${params.toString()}`;
    
    return this.http.get<InventoryMovement[]>(url);
  }

  addEntry(entry: {
    inventoryItemId: string;
    quantity: number;
    unitPrice: number;
    movementDate?: string;
    reference?: string;
    notes?: string;
  }): Observable<any> {
    return this.http.post(`${this.apiUrl}/movements/entry`, entry);
  }

  addExit(exit: {
    inventoryItemId: string;
    quantity: number;
    unitPrice: number;
    movementDate?: string;
    reference?: string;
    notes?: string;
  }): Observable<any> {
    return this.http.post(`${this.apiUrl}/movements/exit`, exit);
  }

  addAdjustment(adjustment: {
    inventoryItemId: string;
    newQuantity: number;
    reason: string;
    movementDate?: string;
    notes?: string;
  }): Observable<any> {
    return this.http.post(`${this.apiUrl}/movements/adjustment`, adjustment);
  }

  createItem(item: {
    name: string;
    description?: string;
    sku?: string;
    initialQuantity?: number;
    minimumQuantity: number;
    maximumQuantity?: number;
    reorderPoint?: number;
    unitPrice: number;
    supplier?: string;
    location?: string;
    batch?: string;
    expirationDate?: string;
  }): Observable<KardexItem> {
    return this.http.post<KardexItem>(`${this.apiUrl}/items`, item);
  }

  updateItem(id: string, item: Partial<KardexItem>): Observable<KardexItem> {
    return this.http.put<KardexItem>(`${this.apiUrl}/items/${id}`, item);
  }

  getKardex(inventoryItemId: string): Observable<KardexReport> {
    return this.http.get<KardexReport>(`${this.apiUrl}/kardex/${inventoryItemId}`);
  }
}
