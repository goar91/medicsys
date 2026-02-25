import { Component, OnInit, computed, signal, inject } from '@angular/core';
import { NgFor, NgIf, DatePipe, DecimalPipe, SlicePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { catchError, forkJoin, of } from 'rxjs';
import {
  AcademicService,
  AcademicClinicalHistory,
  AcademicAppointment,
  AcademicPatient,
  ProfessorClinicalDashboard,
  AcademicCommentTemplate,
  ProfessorPrioritizedReview
} from '../../core/academic.service';
import { TopNavComponent } from '../../shared/top-nav/top-nav';
import { AuthService } from '../../core/auth.service';

interface DashboardMetric {
  label: string;
  value: number | string;
  change: string;
  icon: 'clipboard' | 'check' | 'calendar' | 'users' | 'clock' | 'warning';
}

@Component({
  selector: 'app-professor-dashboard',
  standalone: true,
  imports: [TopNavComponent, NgFor, NgIf, DatePipe, DecimalPipe, SlicePipe, RouterLink, ReactiveFormsModule],
  templateUrl: './professor-dashboard.html',
  styleUrl: './professor-dashboard.scss'
})
export class ProfessorDashboardComponent implements OnInit {
  private readonly academicService = inject(AcademicService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly histories = signal<AcademicClinicalHistory[]>([]);
  readonly appointments = signal<AcademicAppointment[]>([]);
  readonly patients = signal<AcademicPatient[]>([]);
  readonly dashboard = signal<ProfessorClinicalDashboard | null>(null);
  readonly templates = signal<AcademicCommentTemplate[]>([]);
  readonly loading = signal(true);
  readonly batching = signal(false);
  readonly filter = signal<'all' | 'Draft' | 'Submitted' | 'Approved' | 'Rejected'>('all');
  readonly showPatients = signal(false);
  readonly selectedHistoryIds = signal<string[]>([]);
  readonly batchDecision = signal<'approve' | 'reject' | 'requestChanges'>('approve');
  readonly selectedTemplateId = signal('');

  readonly batchNotes = new FormControl('', { nonNullable: true });
  readonly batchGrade = new FormControl<number | null>(null);

  readonly currentUserId = computed(() => this.auth.user()?.id ?? '');
  readonly selectedCount = computed(() => this.selectedHistoryIds().length);
  readonly prioritizedReviewMap = computed(() => {
    const map = new Map<string, ProfessorPrioritizedReview>();
    const queue = this.dashboard()?.prioritizedReviews ?? [];
    for (const item of queue) {
      map.set(item.historyId, item);
    }
    return map;
  });

  readonly priorityQueue = computed<ProfessorPrioritizedReview[]>(() => {
    const queue = this.dashboard()?.prioritizedReviews ?? [];
    if (queue.length > 0) {
      return queue;
    }

    const now = Date.now();
    return this.histories()
      .filter(history => history.status === 'Submitted')
      .map(history => {
        const submittedAt = history.submittedAt ?? history.updatedAt;
        const submittedMs = new Date(submittedAt).getTime();
        const waitingHours = Math.max(0, (now - submittedMs) / (1000 * 60 * 60));
        const slaStatus: ProfessorPrioritizedReview['slaStatus'] =
          waitingHours >= 72 ? 'Critico' : waitingHours >= 48 ? 'EnRiesgo' : 'Normal';

        return {
          historyId: history.id,
          studentId: history.studentId,
          studentName: history.studentName,
          patientName: history.patientName,
          submittedAt,
          hoursWaiting: Number(waitingHours.toFixed(2)),
          recentRejectedCount: 0,
          riskLevel: 'SinBandera',
          slaStatus,
          priorityScore: Number((waitingHours * 0.2).toFixed(2))
        };
      })
      .sort((a, b) => b.priorityScore - a.priorityScore || b.hoursWaiting - a.hoursWaiting)
      .slice(0, 20);
  });

  readonly filtered = computed(() => {
    const value = this.filter();
    const items = this.histories();
    const ordered = [...items].sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime());
    if (value === 'all') {
      return ordered;
    }

    const filtered = ordered.filter(item => item.status === value);
    if (value !== 'Submitted') {
      return filtered;
    }

    const priorityMap = this.prioritizedReviewMap();
    return [...filtered].sort((a, b) => {
      const aPriority = priorityMap.get(a.id)?.priorityScore ?? 0;
      const bPriority = priorityMap.get(b.id)?.priorityScore ?? 0;
      if (aPriority !== bPriority) {
        return bPriority - aPriority;
      }
      return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime();
    });
  });

  readonly selectableIds = computed(() =>
    this.filtered()
      .filter(item => item.status === 'Submitted')
      .map(item => item.id)
  );

  readonly allSelectableChecked = computed(() => {
    const selectable = this.selectableIds();
    if (selectable.length === 0) {
      return false;
    }
    const selected = new Set(this.selectedHistoryIds());
    return selectable.every(id => selected.has(id));
  });

  readonly metrics = computed<DashboardMetric[]>(() => {
    const report = this.dashboard();
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    const todayAppointments = this.appointments().filter(apt => {
      const aptDate = new Date(apt.startAt);
      return aptDate >= today && aptDate < tomorrow;
    }).length;

    return [
      {
        label: 'Historias Pendientes',
        value: report?.pendingReviews ?? 0,
        change: 'Esperan revisión',
        icon: 'clipboard'
      },
      {
        label: 'Tiempo Promedio',
        value: `${(report?.averageApprovalHours ?? 0).toFixed(2)} h`,
        change: 'Aprobación promedio',
        icon: 'clock'
      },
      {
        label: 'Aprobadas',
        value: report?.approvedCount ?? 0,
        change: 'Revisiones aprobadas',
        icon: 'check'
      },
      {
        label: 'Rechazadas',
        value: report?.rejectedCount ?? 0,
        change: 'Solicitudes de cambios',
        icon: 'warning'
      },
      {
        label: 'Citas Hoy',
        value: todayAppointments,
        change: 'Programadas',
        icon: 'calendar'
      },
      {
        label: 'Pacientes',
        value: this.patients().length,
        change: 'Registrados',
        icon: 'users'
      }
    ];
  });

  ngOnInit() {
    this.applyModuleFromRoute();
    this.load();
  }

  load() {
    this.loading.set(true);

    forkJoin({
      histories: this.academicService.getClinicalHistories().pipe(catchError(() => of([] as AcademicClinicalHistory[]))),
      appointments: this.academicService.getAppointments({ professorId: this.currentUserId() }).pipe(catchError(() => of([] as AcademicAppointment[]))),
      patients: this.academicService.getPatients().pipe(catchError(() => of([] as AcademicPatient[]))),
      dashboard: this.academicService.getProfessorClinicalDashboard().pipe(catchError(() => of(null))),
      templates: this.academicService.getCommentTemplates().pipe(catchError(() => of([] as AcademicCommentTemplate[])))
    }).subscribe(result => {
      this.histories.set(result.histories);
      this.appointments.set(result.appointments);
      this.patients.set(result.patients);
      this.dashboard.set(result.dashboard);
      this.templates.set(result.templates);
      this.selectedHistoryIds.set([]);
      this.loading.set(false);
    });
  }

  setFilter(value: 'all' | 'Draft' | 'Submitted' | 'Approved' | 'Rejected') {
    this.filter.set(value);
    this.selectedHistoryIds.set([]);
  }

  toggleHistorySelection(id: string, checked: boolean) {
    const selected = new Set(this.selectedHistoryIds());
    if (checked) {
      selected.add(id);
    } else {
      selected.delete(id);
    }
    this.selectedHistoryIds.set([...selected]);
  }

  toggleSelectAllSubmitted(checked: boolean) {
    if (!checked) {
      this.selectedHistoryIds.set([]);
      return;
    }
    this.selectedHistoryIds.set([...this.selectableIds()]);
  }

  setBatchDecision(decision: 'approve' | 'reject' | 'requestChanges') {
    this.batchDecision.set(decision);
    if (decision !== 'approve') {
      this.batchGrade.setValue(null);
    }
  }

  applyTemplateToBatch(templateId: string) {
    this.selectedTemplateId.set(templateId);
    const template = this.templates().find(item => item.id === templateId);
    if (!template) {
      return;
    }

    const current = this.batchNotes.value?.trim() ?? '';
    const next = current ? `${current}\n${template.commentText}` : template.commentText;
    this.batchNotes.setValue(next);

    this.academicService.markCommentTemplateUsed(templateId).subscribe({
      next: () => {
        this.templates.set(
          this.templates().map(item =>
            item.id === templateId
              ? { ...item, usageCount: item.usageCount + 1, updatedAt: new Date().toISOString() }
              : item
          )
        );
      }
    });
  }

  runBatchAction() {
    if (this.selectedCount() === 0 || this.batching()) {
      return;
    }

    const grade = this.batchGrade.value;
    if (this.batchDecision() === 'approve' && grade != null && (grade < 0 || grade > 10)) {
      alert('La calificación debe estar entre 0 y 10.');
      return;
    }

    this.batching.set(true);
    const templateId = this.selectedTemplateId();
    this.academicService.batchReviewClinicalHistories({
      historyIds: this.selectedHistoryIds(),
      decision: this.batchDecision(),
      reviewNotes: this.batchNotes.value?.trim() || undefined,
      grade: this.batchDecision() === 'approve' ? grade : null,
      templateIds: templateId ? [templateId] : undefined
    }).subscribe({
      next: result => {
        this.batching.set(false);
        this.selectedHistoryIds.set([]);
        this.batchNotes.setValue('');
        this.batchGrade.setValue(null);
        this.selectedTemplateId.set('');
        this.load();
        alert(`Actualizadas: ${result.updated}. Omitidas: ${result.skipped}.`);
      },
      error: () => {
        this.batching.set(false);
      }
    });
  }

  delete(id: string) {
    if (!confirm('¿Seguro que deseas eliminar esta historia clínica?')) {
      return;
    }
    this.academicService.deleteClinicalHistory(id).subscribe({
      next: () => {
        this.histories.set(this.histories().filter(item => item.id !== id));
        this.selectedHistoryIds.set(this.selectedHistoryIds().filter(selectedId => selectedId !== id));
      }
    });
  }

  openHistoriesModule() {
    this.showPatients.set(false);
    if (this.router.url !== '/professor/histories') {
      this.router.navigate(['/professor/histories']);
    }
  }

  openPatientsModule() {
    this.showPatients.set(true);
    if (this.router.url !== '/professor/patients') {
      this.router.navigate(['/professor/patients']);
    }
  }

  deletePatient(id: string) {
    if (!confirm('¿Seguro que deseas eliminar este paciente?')) {
      return;
    }
    this.academicService.deletePatient(id).subscribe({
      next: () => {
        this.patients.set(this.patients().filter(p => p.id !== id));
      }
    });
  }

  private applyModuleFromRoute() {
    const module = this.route.snapshot.data['module'] as string | undefined;
    this.showPatients.set(module === 'patients');
  }

  slaLabel(value: ProfessorPrioritizedReview['slaStatus']) {
    if (value === 'Critico') {
      return 'Crítico >72h';
    }
    if (value === 'EnRiesgo') {
      return 'Riesgo >48h';
    }
    return 'Dentro de SLA';
  }
}
