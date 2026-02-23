import { Component, OnInit, computed, signal, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe, NgFor, NgIf } from '@angular/common';
import { forkJoin } from 'rxjs';
import {
  AcademicService,
  AcademicClinicalHistory,
  AcademicCommentTemplate
} from '../../core/academic.service';
import { TopNavComponent } from '../../shared/top-nav/top-nav';

interface SectionCard {
  title: string;
  items: { label: string; value: string }[];
}

interface ClinicalHistoryData {
  personal?: {
    lastName?: string;
    firstName?: string;
    idNumber?: string;
    gender?: string;
    age?: string;
    ageRange?: string;
    clinicalHistoryNumber?: string;
    address?: string;
    phone?: string;
    date?: string;
  };
  consultation?: {
    reason?: string;
    currentIssue?: string;
    vitalSigns?: {
      bloodPressure?: string;
      heartRate?: string;
      temperature?: string;
      respiratoryRate?: string;
    };
    notes?: string;
  };
  indicators?: {
    higieneOral?: string;
    enfermedadPeriodontal?: string;
    maloclusion?: string;
    fluorosis?: string;
    indiceCpo?: string;
  };
  treatments?: {
    plan?: string;
    procedures?: string;
  };
  medios?: {
    imagenes?: string;
    notas?: string;
    assets?: {
      id?: string;
      fileName?: string;
      url?: string;
      contentType?: string;
      uploadedAt?: string;
    }[];
  };
  odontogram?: {
    teeth?: { code: number; marker: string }[];
  };
}

interface MediaAsset {
  id: string;
  fileName: string;
  url: string;
  contentType: string;
  uploadedAt: string;
}

@Component({
  selector: 'app-clinical-history-review',
  standalone: true,
  imports: [ReactiveFormsModule, NgIf, NgFor, DatePipe, TopNavComponent],
  templateUrl: './clinical-history-review.html',
  styleUrl: './clinical-history-review.scss'
})
export class ClinicalHistoryReviewComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly academic = inject(AcademicService);

  readonly history = signal<AcademicClinicalHistory | null>(null);
  readonly templates = signal<AcademicCommentTemplate[]>([]);
  readonly loading = signal(true);
  readonly reviewing = signal(false);
  readonly deleting = signal(false);
  readonly savingTemplate = signal(false);
  readonly sections = signal<SectionCard[]>([]);
  readonly mediaAssets = signal<MediaAsset[]>([]);
  readonly selectedTemplateId = signal('');
  readonly canReview = computed(() => this.history()?.status === 'Submitted');

  readonly notes = new FormControl('', { nonNullable: true });
  readonly grade = new FormControl<number | null>(null);
  readonly templateTitle = new FormControl('', { nonNullable: true });
  readonly templateCategory = new FormControl('', { nonNullable: true });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    forkJoin({
      history: this.academic.getClinicalHistoryById(id),
      templates: this.academic.getCommentTemplates()
    }).subscribe({
      next: ({ history, templates }) => {
        this.history.set(history);
        this.templates.set(templates);
        this.sections.set(this.buildSections((history.data || {}) as ClinicalHistoryData));
        this.mediaAssets.set(this.extractMediaAssets((history.data || {}) as ClinicalHistoryData));
        this.notes.setValue(history.professorComments ?? '');
        this.grade.setValue(history.grade ?? null);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  approve() {
    this.sendReview(true);
  }

  reject() {
    this.sendReview(false);
  }

  requestChanges() {
    const current = this.notes.value.trim();
    const next = current ? `${current}\nSolicitar ajustes y reenviar.` : 'Solicitar ajustes y reenviar.';
    this.notes.setValue(next);
    this.sendReview(false);
  }

  applyTemplate(templateId: string) {
    this.selectedTemplateId.set(templateId);
    const template = this.templates().find(item => item.id === templateId);
    if (!template) {
      return;
    }

    const current = this.notes.value.trim();
    this.notes.setValue(current ? `${current}\n${template.commentText}` : template.commentText);

    this.academic.markCommentTemplateUsed(templateId).subscribe({
      next: () => {
        this.templates.set(this.templates().map(item =>
          item.id === templateId
            ? { ...item, usageCount: item.usageCount + 1, updatedAt: new Date().toISOString() }
            : item
        ));
      }
    });
  }

  saveTemplateFromNotes() {
    const title = this.templateTitle.value.trim();
    const commentText = this.notes.value.trim();
    if (!title || !commentText || this.savingTemplate()) {
      return;
    }

    this.savingTemplate.set(true);
    this.academic.createCommentTemplate({
      title,
      commentText,
      category: this.templateCategory.value.trim() || undefined
    }).subscribe({
      next: template => {
        this.savingTemplate.set(false);
        this.templates.set([template, ...this.templates()]);
        this.templateTitle.setValue('');
        this.templateCategory.setValue('');
      },
      error: () => {
        this.savingTemplate.set(false);
      }
    });
  }

  deleteTemplate(id: string) {
    this.academic.deleteCommentTemplate(id).subscribe({
      next: () => {
        this.templates.set(this.templates().filter(item => item.id !== id));
      }
    });
  }

  delete() {
    const current = this.history();
    if (!current || this.deleting()) {
      return;
    }
    if (!confirm('¿Seguro que deseas eliminar esta historia clínica?')) {
      return;
    }

    this.deleting.set(true);
    this.academic.deleteClinicalHistory(current.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.router.navigate(['/professor']);
      },
      error: () => {
        this.deleting.set(false);
      }
    });
  }

  isImage(asset: MediaAsset) {
    return asset.contentType?.startsWith('image/');
  }

  isVideo(asset: MediaAsset) {
    return asset.contentType?.startsWith('video/');
  }

  private sendReview(approved: boolean) {
    const current = this.history();
    if (!current || !this.canReview()) {
      return;
    }

    const grade = this.grade.value;
    if (approved && grade != null && (grade < 0 || grade > 10)) {
      alert('La calificación debe estar entre 0 y 10.');
      return;
    }

    this.reviewing.set(true);
    const templateId = this.selectedTemplateId();
    this.academic.reviewClinicalHistory(current.id, {
      approved,
      reviewNotes: this.notes.value.trim() || undefined,
      grade: approved ? grade : null,
      templateIds: templateId ? [templateId] : undefined
    }).subscribe({
      next: history => {
        this.history.set(history);
        this.reviewing.set(false);
        this.router.navigate(['/professor']);
      },
      error: () => {
        this.reviewing.set(false);
      }
    });
  }

  private buildSections(data: ClinicalHistoryData): SectionCard[] {
    return [
      {
        title: 'Datos personales',
        items: [
          { label: 'Apellidos', value: data?.personal?.lastName ?? '-' },
          { label: 'Nombres', value: data?.personal?.firstName ?? '-' },
          { label: 'C.I.', value: data?.personal?.idNumber ?? '-' },
          { label: 'Género', value: data?.personal?.gender ?? '-' },
          { label: 'Edad', value: data?.personal?.age ?? '-' },
          { label: 'Rango de edad', value: data?.personal?.ageRange ?? '-' },
          { label: 'N° historia clínica', value: data?.personal?.clinicalHistoryNumber ?? '-' },
          { label: 'Dirección', value: data?.personal?.address ?? '-' },
          { label: 'Teléfono', value: data?.personal?.phone ?? '-' },
          { label: 'Fecha', value: data?.personal?.date ?? '-' }
        ]
      },
      {
        title: 'Datos de consulta',
        items: [
          { label: 'Motivo de consulta', value: data?.consultation?.reason ?? '-' },
          { label: 'Problema actual', value: data?.consultation?.currentIssue ?? '-' },
          { label: 'Presión arterial', value: data?.consultation?.vitalSigns?.bloodPressure ?? '-' },
          { label: 'Frecuencia cardíaca', value: data?.consultation?.vitalSigns?.heartRate ?? '-' },
          { label: 'Temperatura', value: data?.consultation?.vitalSigns?.temperature ?? '-' },
          { label: 'Frecuencia respiratoria', value: data?.consultation?.vitalSigns?.respiratoryRate ?? '-' },
          { label: 'Observaciones', value: data?.consultation?.notes ?? '-' }
        ]
      },
      {
        title: 'Indicadores',
        items: [
          { label: 'Higiene oral', value: data?.indicators?.higieneOral ?? '-' },
          { label: 'Enfermedad periodontal', value: data?.indicators?.enfermedadPeriodontal ?? '-' },
          { label: 'Mal oclusión', value: data?.indicators?.maloclusion ?? '-' },
          { label: 'Fluorosis', value: data?.indicators?.fluorosis ?? '-' },
          { label: 'Índice CPO-ceo', value: data?.indicators?.indiceCpo ?? '-' }
        ]
      },
      {
        title: 'Tratamientos',
        items: [
          { label: 'Plan', value: data?.treatments?.plan ?? '-' },
          { label: 'Procedimientos', value: data?.treatments?.procedures ?? '-' }
        ]
      },
      {
        title: 'Odontograma',
        items: [
          {
            label: 'Piezas marcadas',
            value: (data?.odontogram?.teeth ?? [])
              .filter(item => item.marker && item.marker !== 'none')
              .map(item => `${item.code} (${item.marker})`)
              .join(', ') || '-'
          }
        ]
      },
      {
        title: 'Medios de apoyo',
        items: [
          { label: 'Imágenes', value: data?.medios?.imagenes ?? '-' },
          { label: 'Notas', value: data?.medios?.notas ?? '-' }
        ]
      }
    ];
  }

  private extractMediaAssets(data: ClinicalHistoryData): MediaAsset[] {
    const assets = data?.medios?.assets;
    if (!Array.isArray(assets)) {
      return [];
    }
    return assets
      .filter(item => item?.url)
      .map(item => ({
        id: item?.id ?? crypto.randomUUID(),
        fileName: item?.fileName ?? 'archivo',
        url: item?.url ?? '',
        contentType: item?.contentType ?? 'application/octet-stream',
        uploadedAt: item?.uploadedAt ?? new Date().toISOString()
      }));
  }
}
