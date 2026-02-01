import { Component, OnInit, computed, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgFor, NgIf } from '@angular/common';
import { ClinicalHistoryService } from '../../core/clinical-history.service';
import { ClinicalHistory } from '../../core/models';
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
    antecedentes?: Record<string, boolean>;
    vitalSigns?: {
      bloodPressure?: string;
      heartRate?: string;
      temperature?: string;
      respiratoryRate?: string;
    };
    estomatognatico?: Record<string, boolean>;
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
  };
  odontogram?: {
    teeth?: { code: number; marker: string }[];
  };
}

@Component({
  selector: 'app-clinical-history-review',
  standalone: true,
  imports: [ReactiveFormsModule, NgIf, NgFor, TopNavComponent],
  templateUrl: './clinical-history-review.html',
  styleUrl: './clinical-history-review.scss'
})
export class ClinicalHistoryReviewComponent implements OnInit {
  readonly history = signal<ClinicalHistory | null>(null);
  readonly loading = signal(true);
  readonly reviewing = signal(false);
  readonly sections = signal<SectionCard[]>([]);
  readonly canReview = computed(() => this.history()?.status === 'Submitted');

  readonly notes = new FormControl('');

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly service: ClinicalHistoryService
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.service.getById(id).subscribe({
      next: history => {
        this.history.set(history);
        this.sections.set(this.buildSections(history.data as ClinicalHistoryData));
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

  private sendReview(approved: boolean) {
    const current = this.history();
    if (!current) {
      return;
    }

    if (!this.canReview()) {
      return;
    }

    this.reviewing.set(true);
    this.service.review(current.id, { approved, notes: this.notes.value ?? '' }).subscribe({
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
          { label: 'Genero', value: data?.personal?.gender ?? '-' },
          { label: 'Edad', value: data?.personal?.age ?? '-' },
          { label: 'Rango de edad', value: data?.personal?.ageRange ?? '-' },
          { label: 'N° historia clinica', value: data?.personal?.clinicalHistoryNumber ?? '-' },
          { label: 'Direccion', value: data?.personal?.address ?? '-' },
          { label: 'Telefono', value: data?.personal?.phone ?? '-' },
          { label: 'Fecha', value: data?.personal?.date ?? '-' }
        ]
      },
      {
        title: 'Datos de consulta',
        items: [
          { label: 'Motivo de consulta', value: data?.consultation?.reason ?? '-' },
          { label: 'Problema actual', value: data?.consultation?.currentIssue ?? '-' },
          { label: 'Presion arterial', value: data?.consultation?.vitalSigns?.bloodPressure ?? '-' },
          { label: 'Frecuencia cardiaca', value: data?.consultation?.vitalSigns?.heartRate ?? '-' },
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
          { label: 'Mal oclusion', value: data?.indicators?.maloclusion ?? '-' },
          { label: 'Fluorosis', value: data?.indicators?.fluorosis ?? '-' },
          { label: 'Indice CPO-ceo', value: data?.indicators?.indiceCpo ?? '-' }
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
          { label: 'Imagenes', value: data?.medios?.imagenes ?? '-' },
          { label: 'Notas', value: data?.medios?.notas ?? '-' }
        ]
      }
    ];
  }
}
