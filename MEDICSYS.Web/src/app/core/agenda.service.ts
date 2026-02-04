import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from './api.config';

export interface Appointment {
  id: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  professorId: string;
  professorName: string;
  professorEmail: string;
  patientName: string;
  reason: string;
  startAt: string;
  endAt: string;
  status: string;
  notes?: string | null;
}

export interface TimeSlot {
  startAt: string;
  endAt: string;
  isAvailable: boolean;
}

export interface AvailabilityResponse {
  date: string;
  timeZone: string;
  slots: TimeSlot[];
}

export interface UserSummary {
  id: string;
  fullName: string;
  email: string;
}

export interface Reminder {
  id: string;
  appointmentId: string;
  channel: string;
  target: string;
  message: string;
  scheduledAt: string;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class AgendaService {
  private readonly baseUrl = `${API_BASE_URL}`;
  private readonly guidRegex =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

  constructor(private readonly http: HttpClient) {}

  private normalizeGuid(value?: string): string | null {
    const trimmed = value?.trim();
    if (!trimmed) return null;
    const lower = trimmed.toLowerCase();
    if (lower === 'null' || lower === 'undefined') return null;
    return this.guidRegex.test(trimmed) ? trimmed : null;
  }

  getAppointments(params?: { studentId?: string; professorId?: string }) {
    let httpParams = new HttpParams();
    const studentId = this.normalizeGuid(params?.studentId);
    if (studentId) {
      httpParams = httpParams.set('studentId', studentId);
    }
    const professorId = this.normalizeGuid(params?.professorId);
    if (professorId) {
      httpParams = httpParams.set('professorId', professorId);
    }
    return this.http.get<Appointment[]>(`${this.baseUrl}/agenda/appointments`, { params: httpParams });
  }

  createAppointment(payload: {
    studentId: string;
    professorId: string;
    patientName: string;
    reason: string;
    startAt: string;
    endAt: string;
    status?: string;
    notes?: string | null;
  }) {
    return this.http.post<Appointment>(`${this.baseUrl}/agenda/appointments`, payload);
  }

  getAvailability(date: string, params?: { studentId?: string; professorId?: string }) {
    let httpParams = new HttpParams().set('date', date);
    const studentId = this.normalizeGuid(params?.studentId);
    if (studentId) {
      httpParams = httpParams.set('studentId', studentId);
    }
    const professorId = this.normalizeGuid(params?.professorId);
    if (professorId) {
      httpParams = httpParams.set('professorId', professorId);
    }
    return this.http.get<AvailabilityResponse>(`${this.baseUrl}/agenda/availability`, { params: httpParams });
  }

  getProfessors() {
    return this.http.get<UserSummary[]>(`${this.baseUrl}/users/professors`);
  }

  getOdontologos() {
    return this.http.get<UserSummary[]>(`${this.baseUrl}/users/odontologos`);
  }

  getStudents() {
    return this.http.get<UserSummary[]>(`${this.baseUrl}/users/students`);
  }

  getReminders(status?: string) {
    let httpParams = new HttpParams();
    if (status) {
      httpParams = httpParams.set('status', status);
    }
    return this.http.get<Reminder[]>(`${this.baseUrl}/reminders`, { params: httpParams });
  }

  updateAppointment(id: string, payload: {
    patientName?: string;
    reason?: string;
    status?: string;
    notes?: string | null;
  }) {
    return this.http.put<Appointment>(`${this.baseUrl}/agenda/appointments/${id}`, payload);
  }

  reviewAppointment(id: string, payload: { approved: boolean; notes?: string }) {
    return this.http.post<Appointment>(`${this.baseUrl}/agenda/appointments/${id}/review`, payload);
  }

  deleteAppointment(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/agenda/appointments/${id}`);
  }
}
