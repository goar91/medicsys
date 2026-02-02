import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ClinicalHistory, ClinicalHistoryReviewRequest } from './models';
import { API_BASE_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class ClinicalHistoryService {
  private readonly baseUrl = `${API_BASE_URL}/clinical-histories`;

  constructor(private readonly http: HttpClient) {}

  getAll() {
    return this.http.get<ClinicalHistory[]>(this.baseUrl);
  }

  getById(id: string) {
    return this.http.get<ClinicalHistory>(`${this.baseUrl}/${id}`);
  }

  create(data: Record<string, unknown>) {
    return this.http.post<ClinicalHistory>(this.baseUrl, { data });
  }

  update(id: string, data: Record<string, unknown>) {
    return this.http.put<ClinicalHistory>(`${this.baseUrl}/${id}`, { data });
  }

  submit(id: string) {
    return this.http.post<ClinicalHistory>(`${this.baseUrl}/${id}/submit`, {});
  }

  review(id: string, payload: ClinicalHistoryReviewRequest) {
    return this.http.post<ClinicalHistory>(`${this.baseUrl}/${id}/review`, payload);
  }

  uploadMedia(id: string, file: File) {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ClinicalHistory>(`${this.baseUrl}/${id}/media`, formData);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
