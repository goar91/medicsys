import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';
import { Patient, PatientCreateRequest } from './patient.model';

@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly baseUrl = `${API_BASE_URL}/patients`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Patient[]> {
    return this.http.get<Patient[]>(this.baseUrl);
  }

  getById(id: string): Observable<Patient> {
    return this.http.get<Patient>(`${this.baseUrl}/${id}`);
  }

  create(data: PatientCreateRequest): Observable<Patient> {
    return this.http.post<Patient>(this.baseUrl, data);
  }

  update(id: string, data: Partial<PatientCreateRequest>): Observable<Patient> {
    return this.http.put<Patient>(`${this.baseUrl}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  search(term: string): Observable<Patient[]> {
    return this.http.get<Patient[]>(`${this.baseUrl}/search?q=${encodeURIComponent(term)}`);
  }
}
