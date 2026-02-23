import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { AiService } from '../../../core/ai.service';

@Component({
  selector: 'app-ai-insights',
  standalone: true,
  imports: [CommonModule, FormsModule, TopNavComponent],
  templateUrl: './ai-insights.component.html',
  styleUrl: './ai-insights.component.scss'
})
export class AiInsightsComponent implements OnInit {
  private readonly ai = inject(AiService);

  diagnosis = signal<{
    primarySuggestion: { diagnosis: string; confidence: number; rationale: string };
    differentialDiagnoses: Array<{ diagnosis: string; confidence: number; rationale: string }>;
    recommendedActions: string[];
    disclaimer: string;
  } | null>(null);

  trends = signal<{
    period: { start: string; end: string; months: number };
    topClinicalPatterns: Array<{ pattern: string; count: number }>;
    appointmentLoadByMonth: Array<{ month: string; appointmentCount: number }>;
    historyRecordsAnalyzed: number;
    insuranceApprovalRate: number;
    forecast: { nextMonthExpectedAppointments: number; basis: string };
    disclaimer: string;
  } | null>(null);

  symptoms = '';
  clinicalFindings = '';
  notes = '';
  months = 6;

  ngOnInit(): void {
    this.loadTrends();
  }

  suggestDiagnosis(): void {
    if (!this.symptoms.trim() && !this.clinicalFindings.trim()) {
      return;
    }

    this.ai.suggestDiagnosis({
      symptoms: this.symptoms,
      clinicalFindings: this.clinicalFindings,
      notes: this.notes || undefined
    }).subscribe({
      next: response => this.diagnosis.set(response),
      error: err => console.error('Error generating diagnosis', err)
    });
  }

  loadTrends(): void {
    this.ai.getPredictiveTrends(this.months).subscribe({
      next: response => this.trends.set(response),
      error: err => console.error('Error loading predictive trends', err)
    });
  }
}
