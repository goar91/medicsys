import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';

export interface TelemedicineSession {
  id: string;
  patientId?: string | null;
  patientName: string;
  topic: string;
  meetingLink?: string | null;
  scheduledStartAt: string;
  scheduledEndAt: string;
  startedAt?: string | null;
  endedAt?: string | null;
  status: string;
  messageCount: number;
}

export interface TelemedicineMessage {
  id: string;
  sessionId: string;
  senderRole: string;
  senderName: string;
  message: string;
  sentAt: string;
}

export interface PortalPatientSummary {
  patient: {
    id: string;
    fullName: string;
    idNumber: string;
    email: string;
    phone: string;
  };
  upcomingAppointments: Array<{
    id: string;
    startAt: string;
    endAt: string;
    reason: string;
    status: string;
  }>;
  invoices: Array<{
    id: string;
    number: string;
    issuedAt: string;
    totalToCharge: number;
    status: string;
  }>;
  clinicalHistoryEntries: Array<{
    id: string;
    createdAt: string;
    updatedAt: string;
    status: string;
  }>;
  preferences: {
    emailEnabled: boolean;
    whatsappEnabled: boolean;
    updatedAt?: string | null;
  };
}

export interface InsuranceClaim {
  id: string;
  patientId: string;
  insurerName: string;
  policyNumber: string;
  procedureCode: string;
  procedureDescription: string;
  requestedAmount: number;
  approvedAmount?: number | null;
  status: string;
  responseMessage?: string | null;
  requestedAt: string;
  resolvedAt?: string | null;
}

export interface SignedDocument {
  id: string;
  patientId: string;
  documentType: string;
  documentName: string;
  documentHash: string;
  signatureProvider: string;
  signatureSerial?: string | null;
  signedAt: string;
  validUntil?: string | null;
  notes?: string | null;
}

@Injectable({ providedIn: 'root' })
export class OdontologoInnovationService {
  private readonly telemedicinaUrl = `${API_BASE_URL}/odontologia/telemedicina`;
  private readonly portalUrl = `${API_BASE_URL}/odontologia/portal-paciente`;
  private readonly segurosUrl = `${API_BASE_URL}/odontologia/seguros`;
  private readonly documentosUrl = `${API_BASE_URL}/odontologia/documentos-firmados`;

  constructor(private readonly http: HttpClient) {}

  getTelemedicineSessions(from?: string, to?: string): Observable<TelemedicineSession[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<TelemedicineSession[]>(`${this.telemedicinaUrl}/sesiones`, { params });
  }

  createTelemedicineSession(payload: {
    patientId?: string | null;
    patientName: string;
    topic: string;
    meetingLink?: string | null;
    scheduledStartAt: string;
    scheduledEndAt: string;
  }): Observable<TelemedicineSession> {
    return this.http.post<TelemedicineSession>(`${this.telemedicinaUrl}/sesiones`, payload);
  }

  startTelemedicineSession(id: string): Observable<TelemedicineSession> {
    return this.http.post<TelemedicineSession>(`${this.telemedicinaUrl}/sesiones/${id}/start`, {});
  }

  endTelemedicineSession(id: string): Observable<TelemedicineSession> {
    return this.http.post<TelemedicineSession>(`${this.telemedicinaUrl}/sesiones/${id}/end`, {});
  }

  cancelTelemedicineSession(id: string): Observable<void> {
    return this.http.post<void>(`${this.telemedicinaUrl}/sesiones/${id}/cancel`, {});
  }

  getTelemedicineMessages(sessionId: string): Observable<TelemedicineMessage[]> {
    return this.http.get<TelemedicineMessage[]>(`${this.telemedicinaUrl}/sesiones/${sessionId}/mensajes`);
  }

  addTelemedicineMessage(sessionId: string, payload: { message: string; senderRole?: string; senderName?: string }): Observable<TelemedicineMessage> {
    return this.http.post<TelemedicineMessage>(`${this.telemedicinaUrl}/sesiones/${sessionId}/mensajes`, payload);
  }

  getPortalPatients(): Observable<Array<{ id: string; fullName: string; idNumber: string; phone: string; email: string }>> {
    return this.http.get<Array<{ id: string; fullName: string; idNumber: string; phone: string; email: string }>>(`${this.portalUrl}/pacientes`);
  }

  getPortalPatientSummary(patientId: string): Observable<PortalPatientSummary> {
    return this.http.get<PortalPatientSummary>(`${this.portalUrl}/pacientes/${patientId}/resumen`);
  }

  updatePortalPreferences(patientId: string, emailEnabled: boolean, whatsAppEnabled: boolean): Observable<{ id: string }> {
    return this.http.put<{ id: string }>(`${this.portalUrl}/pacientes/${patientId}/preferencias`, {
      emailEnabled,
      whatsAppEnabled
    });
  }

  scheduleReminder(patientId: string, payload: {
    message: string;
    subject?: string;
    scheduledFor?: string;
    sendEmail: boolean;
    sendWhatsApp: boolean;
  }): Observable<Array<{ id: string }>> {
    return this.http.post<Array<{ id: string }>>(`${this.portalUrl}/pacientes/${patientId}/recordatorios`, payload);
  }

  getNotifications(patientId?: string, status?: string): Observable<Array<{ id: string; status: string }>> {
    let params = new HttpParams();
    if (patientId) params = params.set('patientId', patientId);
    if (status) params = params.set('status', status);
    return this.http.get<Array<{ id: string; status: string }>>(`${this.portalUrl}/notificaciones`, { params });
  }

  markNotificationAsSent(id: string): Observable<void> {
    return this.http.post<void>(`${this.portalUrl}/notificaciones/${id}/marcar-enviado`, {});
  }

  validateCoverage(payload: {
    patientId: string;
    insurerName: string;
    policyNumber: string;
    procedureCode: string;
    requestedAmount: number;
  }): Observable<{
    insurer: string;
    policyNumber: string;
    procedureCode: string;
    requestedAmount: number;
    coveragePercent: number;
    coveredAmount: number;
    isApproved: boolean;
    message: string;
  }> {
    return this.http.post<{
      insurer: string;
      policyNumber: string;
      procedureCode: string;
      requestedAmount: number;
      coveragePercent: number;
      coveredAmount: number;
      isApproved: boolean;
      message: string;
    }>(`${this.segurosUrl}/validar-cobertura`, payload);
  }

  getClaims(patientId?: string, status?: string): Observable<InsuranceClaim[]> {
    let params = new HttpParams();
    if (patientId) params = params.set('patientId', patientId);
    if (status) params = params.set('status', status);
    return this.http.get<InsuranceClaim[]>(`${this.segurosUrl}/reclamaciones`, { params });
  }

  createClaim(payload: {
    patientId: string;
    insurerName: string;
    policyNumber: string;
    procedureCode: string;
    procedureDescription: string;
    requestedAmount: number;
  }): Observable<InsuranceClaim> {
    return this.http.post<InsuranceClaim>(`${this.segurosUrl}/reclamaciones`, payload);
  }

  updateClaimStatus(id: string, payload: {
    status: string;
    approvedAmount?: number | null;
    responseMessage?: string;
  }): Observable<InsuranceClaim> {
    return this.http.put<InsuranceClaim>(`${this.segurosUrl}/reclamaciones/${id}/estado`, payload);
  }

  getSignedDocuments(patientId?: string): Observable<SignedDocument[]> {
    let params = new HttpParams();
    if (patientId) params = params.set('patientId', patientId);
    return this.http.get<SignedDocument[]>(this.documentosUrl, { params });
  }

  createSignedDocument(payload: {
    patientId: string;
    documentType: string;
    documentName: string;
    signatureProvider: string;
    signatureSerial?: string;
    documentContent?: string;
    notes?: string;
    signedAt?: string;
    validUntil?: string;
  }): Observable<SignedDocument> {
    return this.http.post<SignedDocument>(this.documentosUrl, payload);
  }
}
