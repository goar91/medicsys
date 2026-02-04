import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';

export interface AcademicAppointment {
  id: string;
  studentId: string;
  professorId: string;
  patientName: string;
  reason: string;
  startAt: string;
  endAt: string;
  status: string;
  notes?: string;
  createdAt: string;
}

export interface AcademicClinicalHistory {
  id: string;
  studentId: string;
  patientName: string;
  patientIdNumber: string;
  data: Record<string, unknown>;
  status: 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
  reviewedBy?: string;
  reviewNotes?: string;
  reviewedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface AcademicReminder {
  id: string;
  appointmentId: string;
  target: string;
  message: string;
  channel: 'Email' | 'SMS' | 'Push';
  scheduledAt: string;
  sentAt?: string;
  status: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class AcademicService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/academic`;

  // Citas académicas
  getAppointments(params?: { studentId?: string; professorId?: string }): Observable<AcademicAppointment[]> {
    return this.http.get<AcademicAppointment[]>(`${this.baseUrl}/appointments`, { params: params as any });
  }

  createAppointment(data: Partial<AcademicAppointment>): Observable<AcademicAppointment> {
    return this.http.post<AcademicAppointment>(`${this.baseUrl}/appointments`, data);
  }

  updateAppointment(id: string, data: Partial<AcademicAppointment>): Observable<AcademicAppointment> {
    return this.http.put<AcademicAppointment>(`${this.baseUrl}/appointments/${id}`, data);
  }

  deleteAppointment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/appointments/${id}`);
  }

  // Historias clínicas académicas
  getClinicalHistories(params?: { studentId?: string; status?: string }): Observable<AcademicClinicalHistory[]> {
    return this.http.get<AcademicClinicalHistory[]>(`${this.baseUrl}/clinical-histories`, { params: params as any });
  }

  getClinicalHistoryById(id: string): Observable<AcademicClinicalHistory> {
    return this.http.get<AcademicClinicalHistory>(`${this.baseUrl}/clinical-histories/${id}`);
  }

  createClinicalHistory(data: Partial<AcademicClinicalHistory>): Observable<AcademicClinicalHistory> {
    return this.http.post<AcademicClinicalHistory>(`${this.baseUrl}/clinical-histories`, data);
  }

  updateClinicalHistory(id: string, data: Partial<AcademicClinicalHistory>): Observable<AcademicClinicalHistory> {
    return this.http.put<AcademicClinicalHistory>(`${this.baseUrl}/clinical-histories/${id}`, data);
  }

  submitClinicalHistory(id: string): Observable<AcademicClinicalHistory> {
    return this.http.post<AcademicClinicalHistory>(`${this.baseUrl}/clinical-histories/${id}/submit`, {});
  }

  reviewClinicalHistory(id: string, approved: boolean, notes?: string): Observable<AcademicClinicalHistory> {
    return this.http.post<AcademicClinicalHistory>(`${this.baseUrl}/clinical-histories/${id}/review`, {
      approved,
      reviewNotes: notes
    });
  }

  deleteClinicalHistory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/clinical-histories/${id}`);
  }

  // Recordatorios
  getReminders(params?: { appointmentId?: string }): Observable<AcademicReminder[]> {
    return this.http.get<AcademicReminder[]>(`${this.baseUrl}/reminders`, { params: params as any });
  }
}
