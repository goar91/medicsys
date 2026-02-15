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

export interface AcademicPatient {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  idNumber: string;
  dateOfBirth: string;
  gender: string;
  phone?: string;
  email?: string;
  address?: string;
  bloodType?: string;
  allergies?: string;
  medicalConditions?: string;
  emergencyContact?: string;
  emergencyPhone?: string;
  createdByProfessorName: string;
  createdAt: string;
  updatedAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class AcademicService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/academic`;
  private readonly guidRegex =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

  private normalizeGuid(value?: string): string | null {
    const trimmed = value?.trim();
    if (!trimmed) return null;
    const lower = trimmed.toLowerCase();
    if (lower === 'null' || lower === 'undefined') return null;
    return this.guidRegex.test(trimmed) ? trimmed : null;
  }

  // Citas académicas
  getAppointments(params?: { studentId?: string; professorId?: string }): Observable<AcademicAppointment[]> {
    const httpParams: Record<string, string> = {};
    const studentId = this.normalizeGuid(params?.studentId);
    const professorId = this.normalizeGuid(params?.professorId);
    if (studentId) httpParams['studentId'] = studentId;
    if (professorId) httpParams['professorId'] = professorId;
    return this.http.get<AcademicAppointment[]>(`${this.baseUrl}/appointments`, { params: httpParams as any });
  }

  createAppointment(data: Partial<AcademicAppointment>): Observable<AcademicAppointment> {
    return this.http.post<AcademicAppointment>(`${this.baseUrl}/appointments`, data);
  }

  updateAppointment(id: string, data: Partial<AcademicAppointment>): Observable<AcademicAppointment> {
    return this.http.put<AcademicAppointment>(`${this.baseUrl}/appointments/${id}`, data);
  }

  reviewAppointment(id: string, approved: boolean, notes?: string): Observable<AcademicAppointment> {
    return this.http.post<AcademicAppointment>(`${this.baseUrl}/appointments/${id}/review`, {
      approved,
      notes
    });
  }

  deleteAppointment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/appointments/${id}`);
  }

  // Historias clínicas académicas
  getClinicalHistories(params?: { studentId?: string; status?: string }): Observable<AcademicClinicalHistory[]> {
    const httpParams: Record<string, string> = {};
    const studentId = this.normalizeGuid(params?.studentId);
    if (studentId) httpParams['studentId'] = studentId;
    if (params?.status) httpParams['status'] = params.status;
    return this.http.get<AcademicClinicalHistory[]>(`${this.baseUrl}/clinical-histories`, { params: httpParams as any });
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

  // Pacientes académicos
  getPatients(search?: string): Observable<AcademicPatient[]> {
    const params: any = search ? { search } : undefined;
    return this.http.get<AcademicPatient[]>(`${this.baseUrl}/patients`, { params });
  }

  getPatientById(id: string): Observable<AcademicPatient> {
    return this.http.get<AcademicPatient>(`${this.baseUrl}/patients/${id}`);
  }

  createPatient(data: Partial<AcademicPatient>): Observable<AcademicPatient> {
    return this.http.post<AcademicPatient>(`${this.baseUrl}/patients`, data);
  }

  updatePatient(id: string, data: Partial<AcademicPatient>): Observable<AcademicPatient> {
    return this.http.put<AcademicPatient>(`${this.baseUrl}/patients/${id}`, data);
  }

  deletePatient(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/patients/${id}`);
  }
}
