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
}
