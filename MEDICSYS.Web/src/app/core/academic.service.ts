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
  studentName: string;
  patientName: string;
  patientIdNumber: string;
  data: Record<string, unknown>;
  status: 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
  reviewedByProfessorId?: string;
  reviewedByProfessorName?: string;
  professorComments?: string;
  grade?: number | null;
  submittedAt?: string | null;
  reviewedAt?: string | null;
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

export interface ProfessorCommonError {
  comment: string;
  count: number;
}

export interface ProfessorErrorByStudent {
  studentName: string;
  count: number;
}

export interface ProfessorErrorByGroup {
  groupName: string;
  count: number;
}

export interface StudentProgress {
  studentId: string;
  studentName: string;
  groupName?: string | null;
  totalHistories: number;
  pendingReviews: number;
  approvedHistories: number;
  rejectedHistories: number;
  averageGrade?: number | null;
  progressPercent: number;
}

export interface ProfessorClinicalDashboard {
  pendingReviews: number;
  totalReviewed: number;
  approvedCount: number;
  rejectedCount: number;
  averageApprovalHours: number;
  commonErrors: ProfessorCommonError[];
  errorsByStudent: ProfessorErrorByStudent[];
  errorsByGroup: ProfessorErrorByGroup[];
  studentProgress: StudentProgress[];
}

export interface AcademicCommentTemplate {
  id: string;
  title: string;
  commentText: string;
  category?: string | null;
  usageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface BatchReviewResponse {
  requested: number;
  updated: number;
  skipped: number;
  updatedIds: string[];
  skippedItems: Array<{ historyId: string; reason: string }>;
}

export interface CacesDashboard {
  totalCriteria: number;
  compliantCriteria: number;
  inProgressCriteria: number;
  needsImprovementCriteria: number;
  complianceRate: number;
  overdueActions: number;
  pendingEvidenceVerification: number;
  atRiskCriteria: CacesAtRiskCriterion[];
}

export interface CacesAtRiskCriterion {
  id: string;
  code: string;
  name: string;
  dimension: string;
  targetValue: number;
  currentValue: number;
  status: string;
}

export interface CacesCriterion {
  id: string;
  code: string;
  name: string;
  dimension: string;
  description?: string | null;
  targetValue: number;
  currentValue: number;
  status: string;
  evidenceCount: number;
  verifiedEvidenceCount: number;
  improvementActionsCount: number;
  completedImprovementActionsCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ComplianceDashboard {
  totalConsents: number;
  grantedConsents: number;
  revokedConsents: number;
  pendingAnonymizationRequests: number;
  approvedAnonymizationRequests: number;
  completedAnonymizationRequests: number;
  activeRetentionPolicies: number;
  subjectsWithConsentOlderThan2Years: number;
  recentAuditEvents: ComplianceAuditEvent[];
}

export interface ComplianceAuditEvent {
  id: string;
  eventType: string;
  path: string;
  method: string;
  statusCode: number;
  userEmail?: string | null;
  userRole?: string | null;
  subjectType?: string | null;
  subjectIdentifier?: string | null;
  occurredAt: string;
}

export interface ComplianceConsent {
  id: string;
  subjectType: string;
  subjectId?: string | null;
  subjectIdentifier: string;
  purpose: string;
  legalBasis: string;
  granted: boolean;
  grantedAt: string;
  revokedAt?: string | null;
  collectedByUserId: string;
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface AnonymizationRequest {
  id: string;
  subjectType: string;
  subjectId?: string | null;
  subjectIdentifier: string;
  reason: string;
  status: string;
  requestedByUserId: string;
  reviewedByUserId?: string | null;
  requestedAt: string;
  reviewedAt?: string | null;
  completedAt?: string | null;
  resolutionNotes?: string | null;
}

export interface RetentionPolicy {
  id: string;
  dataCategory: string;
  retentionMonths: number;
  autoDelete: boolean;
  isActive: boolean;
  configuredByUserId: string;
  createdAt: string;
  updatedAt: string;
}

export interface AcademicAnalyticsDashboard {
  totalStudents: number;
  highRiskStudents: number;
  mediumRiskStudents: number;
  approvalRate: number;
  pendingHistories: number;
  pendingAppointments: number;
  cohortMetrics: CohortMetric[];
  topRiskStudents: StudentRiskProfile[];
  professorPerformance: ProfessorPerformanceMetric[];
}

export interface CohortMetric {
  cohort: string;
  students: number;
  highRiskStudents: number;
  averageProgressPercent: number;
}

export interface StudentRiskProfile {
  studentId: string;
  studentName: string;
  studentEmail: string;
  cohort: string;
  totalHistories: number;
  approvedHistories: number;
  rejectedHistories: number;
  pendingHistories: number;
  totalAppointments: number;
  overdueAppointments: number;
  activeManualFlags: number;
  riskScore: number;
  riskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
  progressPercent: number;
}

export interface ProfessorPerformanceMetric {
  professorId: string;
  professorName: string;
  totalReviewed: number;
  approvedReviewed: number;
  rejectedReviewed: number;
  averageGrade?: number | null;
}

export interface RiskFlag {
  id: string;
  studentId: string;
  riskLevel: string;
  notes: string;
  isResolved: boolean;
  createdByUserId: string;
  createdAt: string;
  resolvedAt?: string | null;
  resolvedByUserId?: string | null;
}

export interface IntegrationConnector {
  id: string;
  name: string;
  providerType: string;
  endpointUrl?: string | null;
  apiKeyHint?: string | null;
  enabled: boolean;
  lastSyncAt?: string | null;
  lastStatus?: string | null;
  lastMessage?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface IntegrationSyncLog {
  id: string;
  integrationId: string;
  startedAt: string;
  endedAt?: string | null;
  status: string;
  recordsProcessed: number;
  message?: string | null;
}

export interface IntegrationTestResult {
  integrationId: string;
  integrationName: string;
  providerType: string;
  success: boolean;
  message?: string | null;
  processedAt: string;
  syncStatus: string;
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

  createClinicalHistory(data: Record<string, unknown>): Observable<AcademicClinicalHistory> {
    return this.http.post<AcademicClinicalHistory>(`${this.baseUrl}/clinical-histories`, { data });
  }

  updateClinicalHistory(id: string, data: Record<string, unknown>): Observable<AcademicClinicalHistory> {
    return this.http.put<AcademicClinicalHistory>(`${this.baseUrl}/clinical-histories/${id}`, { data });
  }

  submitClinicalHistory(id: string): Observable<AcademicClinicalHistory> {
    return this.http.post<AcademicClinicalHistory>(`${this.baseUrl}/clinical-histories/${id}/submit`, {});
  }

  reviewClinicalHistory(id: string, payload: {
    approved: boolean;
    reviewNotes?: string;
    grade?: number | null;
    templateIds?: string[];
  }): Observable<AcademicClinicalHistory> {
    return this.http.post<AcademicClinicalHistory>(`${this.baseUrl}/clinical-histories/${id}/review`, payload);
  }

  batchReviewClinicalHistories(payload: {
    historyIds: string[];
    decision: 'approve' | 'reject' | 'requestChanges';
    reviewNotes?: string;
    grade?: number | null;
    templateIds?: string[];
  }): Observable<BatchReviewResponse> {
    return this.http.post<BatchReviewResponse>(`${this.baseUrl}/clinical-histories/batch-review`, payload);
  }

  deleteClinicalHistory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/clinical-histories/${id}`);
  }

  getProfessorClinicalDashboard(): Observable<ProfessorClinicalDashboard> {
    return this.http.get<ProfessorClinicalDashboard>(`${this.baseUrl}/clinical-histories/dashboard`);
  }

  getCommentTemplates(): Observable<AcademicCommentTemplate[]> {
    return this.http.get<AcademicCommentTemplate[]>(`${this.baseUrl}/clinical-histories/comment-templates`);
  }

  createCommentTemplate(payload: {
    title: string;
    commentText: string;
    category?: string;
  }): Observable<AcademicCommentTemplate> {
    return this.http.post<AcademicCommentTemplate>(`${this.baseUrl}/clinical-histories/comment-templates`, payload);
  }

  updateCommentTemplate(id: string, payload: {
    title: string;
    commentText: string;
    category?: string;
  }): Observable<AcademicCommentTemplate> {
    return this.http.put<AcademicCommentTemplate>(`${this.baseUrl}/clinical-histories/comment-templates/${id}`, payload);
  }

  deleteCommentTemplate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/clinical-histories/comment-templates/${id}`);
  }

  markCommentTemplateUsed(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/clinical-histories/comment-templates/${id}/use`, {});
  }

  getReminders(params?: { appointmentId?: string }): Observable<AcademicReminder[]> {
    return this.http.get<AcademicReminder[]>(`${this.baseUrl}/reminders`, { params: params as any });
  }

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

  getCacesDashboard(): Observable<CacesDashboard> {
    return this.http.get<CacesDashboard>(`${this.baseUrl}/caces/dashboard`);
  }

  getCacesCriteria(dimension?: string): Observable<CacesCriterion[]> {
    const params: any = dimension ? { dimension } : undefined;
    return this.http.get<CacesCriterion[]>(`${this.baseUrl}/caces/criteria`, { params });
  }

  createCacesCriterion(payload: {
    code: string;
    name: string;
    dimension: string;
    description?: string;
    targetValue: number;
    currentValue: number;
    status?: string;
  }): Observable<CacesCriterion> {
    return this.http.post<CacesCriterion>(`${this.baseUrl}/caces/criteria`, payload);
  }

  updateCacesCriterion(id: string, payload: {
    code?: string;
    name?: string;
    dimension?: string;
    description?: string;
    targetValue: number;
    currentValue: number;
    status?: string;
  }): Observable<CacesCriterion> {
    return this.http.put<CacesCriterion>(`${this.baseUrl}/caces/criteria/${id}`, payload);
  }

  addCacesEvidence(criterionId: string, payload: {
    title: string;
    description?: string;
    sourceType?: string;
    evidenceUrl?: string;
  }): Observable<any> {
    return this.http.post(`${this.baseUrl}/caces/criteria/${criterionId}/evidences`, payload);
  }

  updateCacesEvidenceVerification(evidenceId: string, isVerified: boolean): Observable<any> {
    return this.http.put(`${this.baseUrl}/caces/evidences/${evidenceId}/verify`, { isVerified });
  }

  addCacesImprovementAction(criterionId: string, payload: {
    action: string;
    responsible: string;
    dueDate: string;
    progressPercent: number;
    status?: string;
  }): Observable<any> {
    return this.http.post(`${this.baseUrl}/caces/criteria/${criterionId}/improvement-actions`, payload);
  }

  updateCacesImprovementAction(actionId: string, payload: {
    action?: string;
    responsible?: string;
    dueDate?: string;
    progressPercent?: number;
    status?: string;
  }): Observable<any> {
    return this.http.put(`${this.baseUrl}/caces/improvement-actions/${actionId}`, payload);
  }

  getComplianceDashboard(): Observable<ComplianceDashboard> {
    return this.http.get<ComplianceDashboard>(`${this.baseUrl}/compliance/dashboard`);
  }

  getConsents(params?: { subjectType?: string; subjectIdentifier?: string }): Observable<ComplianceConsent[]> {
    return this.http.get<ComplianceConsent[]>(`${this.baseUrl}/compliance/consents`, { params: params as any });
  }

  createConsent(payload: {
    subjectType: string;
    subjectId?: string;
    subjectIdentifier: string;
    purpose: string;
    legalBasis: string;
    granted: boolean;
    grantedAt?: string;
    notes?: string;
  }): Observable<ComplianceConsent> {
    return this.http.post<ComplianceConsent>(`${this.baseUrl}/compliance/consents`, payload);
  }

  revokeConsent(id: string, reason?: string): Observable<ComplianceConsent> {
    return this.http.put<ComplianceConsent>(`${this.baseUrl}/compliance/consents/${id}/revoke`, { reason });
  }

  getAnonymizationRequests(status?: string): Observable<AnonymizationRequest[]> {
    const params: any = status ? { status } : undefined;
    return this.http.get<AnonymizationRequest[]>(`${this.baseUrl}/compliance/anonymization-requests`, { params });
  }

  createAnonymizationRequest(payload: {
    subjectType: string;
    subjectId?: string;
    subjectIdentifier: string;
    reason: string;
  }): Observable<AnonymizationRequest> {
    return this.http.post<AnonymizationRequest>(`${this.baseUrl}/compliance/anonymization-requests`, payload);
  }

  reviewAnonymizationRequest(id: string, approved: boolean, notes?: string): Observable<AnonymizationRequest> {
    return this.http.put<AnonymizationRequest>(`${this.baseUrl}/compliance/anonymization-requests/${id}/review`, { approved, notes });
  }

  completeAnonymizationRequest(id: string, maskPrefix?: string): Observable<AnonymizationRequest> {
    return this.http.put<AnonymizationRequest>(`${this.baseUrl}/compliance/anonymization-requests/${id}/complete`, { maskPrefix });
  }

  getRetentionPolicies(): Observable<RetentionPolicy[]> {
    return this.http.get<RetentionPolicy[]>(`${this.baseUrl}/compliance/retention-policies`);
  }

  createRetentionPolicy(payload: {
    dataCategory: string;
    retentionMonths: number;
    autoDelete: boolean;
    isActive: boolean;
  }): Observable<RetentionPolicy> {
    return this.http.post<RetentionPolicy>(`${this.baseUrl}/compliance/retention-policies`, payload);
  }

  updateRetentionPolicy(id: string, payload: {
    dataCategory?: string;
    retentionMonths: number;
    autoDelete: boolean;
    isActive: boolean;
  }): Observable<RetentionPolicy> {
    return this.http.put<RetentionPolicy>(`${this.baseUrl}/compliance/retention-policies/${id}`, payload);
  }

  getAuditEvents(take = 200): Observable<ComplianceAuditEvent[]> {
    return this.http.get<ComplianceAuditEvent[]>(`${this.baseUrl}/compliance/audit-events`, { params: { take } as any });
  }

  getAnalyticsDashboard(): Observable<AcademicAnalyticsDashboard> {
    return this.http.get<AcademicAnalyticsDashboard>(`${this.baseUrl}/analytics/dashboard`);
  }

  getDropoutRiskProfiles(): Observable<StudentRiskProfile[]> {
    return this.http.get<StudentRiskProfile[]>(`${this.baseUrl}/analytics/dropout-risk`);
  }

  createRiskFlag(payload: {
    studentId: string;
    riskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
    notes: string;
  }): Observable<RiskFlag> {
    return this.http.post<RiskFlag>(`${this.baseUrl}/analytics/risk-flags`, payload);
  }

  resolveRiskFlag(id: string, resolutionNotes?: string): Observable<RiskFlag> {
    return this.http.put<RiskFlag>(`${this.baseUrl}/analytics/risk-flags/${id}/resolve`, { resolutionNotes });
  }

  getIntegrations(): Observable<IntegrationConnector[]> {
    return this.http.get<IntegrationConnector[]>(`${this.baseUrl}/integrations`);
  }

  getIntegrationLogs(id: string, take = 100): Observable<IntegrationSyncLog[]> {
    return this.http.get<IntegrationSyncLog[]>(`${this.baseUrl}/integrations/${id}/logs`, { params: { take } as any });
  }

  createIntegration(payload: {
    name: string;
    providerType: string;
    endpointUrl?: string;
    apiKeyHint?: string;
    enabled: boolean;
  }): Observable<IntegrationConnector> {
    return this.http.post<IntegrationConnector>(`${this.baseUrl}/integrations`, payload);
  }

  updateIntegration(id: string, payload: {
    name?: string;
    providerType: string;
    endpointUrl?: string;
    apiKeyHint?: string;
    enabled: boolean;
  }): Observable<IntegrationConnector> {
    return this.http.put<IntegrationConnector>(`${this.baseUrl}/integrations/${id}`, payload);
  }

  testIntegration(id: string): Observable<IntegrationTestResult> {
    return this.http.post<IntegrationTestResult>(`${this.baseUrl}/integrations/${id}/test`, {});
  }

  syncIntegration(id: string): Observable<IntegrationTestResult> {
    return this.http.post<IntegrationTestResult>(`${this.baseUrl}/integrations/${id}/sync`, {});
  }
}
