import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ClinicalHistory, ClinicalHistoryReviewRequest } from './models';
import { API_BASE_URL } from './api.config';
import { Subject, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ClinicalHistoryService {
  private readonly baseUrl = `${API_BASE_URL}/clinical-histories`;
  private readonly historyChangedSubject = new Subject<void>();
  readonly historyChanged$ = this.historyChangedSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  getAll() {
    return this.http.get<ClinicalHistory[]>(this.baseUrl);
  }

  getById(id: string) {
    return this.http.get<ClinicalHistory>(`${this.baseUrl}/${id}`);
  }

  create(data: Record<string, unknown>) {
    return this.http.post<ClinicalHistory>(this.baseUrl, { data }).pipe(
      tap(() => this.historyChangedSubject.next())
    );
  }

  update(id: string, data: Record<string, unknown>) {
    return this.http.put<ClinicalHistory>(`${this.baseUrl}/${id}`, { data }).pipe(
      tap(() => this.historyChangedSubject.next())
    );
  }

  submit(id: string) {
    return this.http.post<ClinicalHistory>(`${this.baseUrl}/${id}/submit`, {}).pipe(
      tap(() => this.historyChangedSubject.next())
    );
  }

  review(id: string, payload: ClinicalHistoryReviewRequest) {
    return this.http.post<ClinicalHistory>(`${this.baseUrl}/${id}/review`, payload).pipe(
      tap(() => this.historyChangedSubject.next())
    );
  }

  uploadMedia(id: string, file: File) {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ClinicalHistory>(`${this.baseUrl}/${id}/media`, formData).pipe(
      tap(() => this.historyChangedSubject.next())
    );
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
      tap(() => this.historyChangedSubject.next())
    );
  }
}
