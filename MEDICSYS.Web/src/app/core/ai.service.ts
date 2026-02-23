import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class AiService {
  constructor(private readonly http: HttpClient) {}

  suggestNotes(payload: {
    reason?: string;
    currentIssue?: string;
    notes?: string;
    plan?: string;
    procedures?: string;
  }) {
    return this.http.post<{ suggestion: string }>(`${API_BASE_URL}/ai/suggest-notes`, payload);
  }

  suggestDiagnosis(payload: {
    symptoms: string;
    clinicalFindings: string;
    notes?: string;
  }) {
    return this.http.post<{
      primarySuggestion: { diagnosis: string; confidence: number; rationale: string };
      differentialDiagnoses: Array<{ diagnosis: string; confidence: number; rationale: string }>;
      recommendedActions: string[];
      disclaimer: string;
    }>(`${API_BASE_URL}/ai/suggest-diagnosis`, payload);
  }

  getPredictiveTrends(months: number = 6) {
    return this.http.get<{
      period: { start: string; end: string; months: number };
      topClinicalPatterns: Array<{ pattern: string; count: number }>;
      appointmentLoadByMonth: Array<{ month: string; appointmentCount: number }>;
      historyRecordsAnalyzed: number;
      insuranceApprovalRate: number;
      forecast: { nextMonthExpectedAppointments: number; basis: string };
      disclaimer: string;
    }>(`${API_BASE_URL}/ai/predictive-trends?months=${months}`);
  }
}
