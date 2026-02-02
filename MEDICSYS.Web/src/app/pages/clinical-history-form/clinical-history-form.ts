import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Observable, of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { ClinicalHistoryService } from '../../core/clinical-history.service';
import { ClinicalHistory } from '../../core/models';
import { AuthService } from '../../core/auth.service';
import { AiService } from '../../core/ai.service';
import { TopNavComponent } from '../../shared/top-nav/top-nav';
import { Odontogram3DComponent, Odontogram3DState, OdontogramMarker, OdontogramSurface } from '../../shared/odontogram-3d/odontogram-3d';

type OdontogramState = Odontogram3DState;

interface MediaAsset {
  id: string;
  fileName: string;
  url: string;
  contentType: string;
  uploadedAt: string;
}

@Component({
  selector: 'app-clinical-history-form',
  standalone: true,
  imports: [ReactiveFormsModule, NgIf, NgFor, DatePipe, TopNavComponent, Odontogram3DComponent],
  templateUrl: './clinical-history-form.html',
  styleUrl: './clinical-history-form.scss'
})
export class ClinicalHistoryFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(ClinicalHistoryService);
  private readonly auth = inject(AuthService);
  private readonly ai = inject(AiService);

  readonly history = signal<ClinicalHistory | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly suggesting = signal(false);
  readonly isProfessorEditor = signal(false);
  readonly activeTab = signal<'personal' | 'consulta' | 'odontograma' | 'indicadores' | 'tratamientos' | 'medios'>('personal');
  readonly marker = signal<OdontogramMarker>('caries');
  readonly estomatognaticoSelected = signal<string | null>(null);
  readonly readonly = computed(() => {
    if (this.isProfessorEditor()) {
      return false;
    }
    const status = this.history()?.status;
    return status === 'Submitted' || status === 'Approved';
  });

  readonly odontogram = signal<OdontogramState>({
    teeth: this.buildDefaultTeeth(),
    depths: {},
    neckLevels: this.buildDefaultNeckLevels(),
    showNumbers: true,
    showArchLabels: true
  });
  readonly mediaAssets = signal<MediaAsset[]>([]);
  readonly uploadingMedia = signal(false);

  readonly form = this.fb.nonNullable.group({
    personal: this.fb.nonNullable.group({
      lastName: ['', [Validators.required]],
      firstName: ['', [Validators.required]],
      idNumber: ['', [Validators.required]],
      gender: ['', [Validators.required]],
      age: ['', [Validators.required]],
      ageRange: ['', [Validators.required]],
      clinicalHistoryNumber: ['', [Validators.required]],
      address: ['', [Validators.required]],
      phone: ['', [Validators.required]],
      date: ['', [Validators.required]]
    }),
    consultation: this.fb.nonNullable.group({
      reason: ['', [Validators.required]],
      currentIssue: ['', [Validators.required]],
      antecedentes: this.fb.nonNullable.group({
        alergiaAntibiotico: false,
        alergiaAnestesia: false,
        hemorragias: false,
        vih: false,
        tuberculosis: false,
        asma: false,
        diabetes: false,
        hipertension: false,
        cardiaca: false,
        otros: false
      }),
      vitalSigns: this.fb.nonNullable.group({
        bloodPressure: ['', [Validators.required]],
        heartRate: ['', [Validators.required]],
        temperature: ['', [Validators.required]],
        respiratoryRate: ['', [Validators.required]]
      }),
      estomatognatico: this.fb.nonNullable.group({
        labios: false,
        mejillas: false,
        maxilarSuperior: false,
        maxilarInferior: false,
        lengua: false,
        paladar: false,
        piso: false,
        carrillos: false,
        glandulasSalivales: false,
        orofaringe: false,
        atm: false,
        ganglios: false
      }),
      estomatognaticoDetails: this.fb.nonNullable.group({
        labios: '',
        mejillas: '',
        maxilarSuperior: '',
        maxilarInferior: '',
        lengua: '',
        paladar: '',
        piso: '',
        carrillos: '',
        glandulasSalivales: '',
        orofaringe: '',
        atm: '',
        ganglios: ''
      }),
      notes: ['', [Validators.required]]
    }),
    indicators: this.fb.nonNullable.group({
      higieneOral: ['', [Validators.required]],
      enfermedadPeriodontal: ['', [Validators.required]],
      maloclusion: ['', [Validators.required]],
      fluorosis: ['', [Validators.required]],
      indiceCpo: ['', [Validators.required]]
    }),
    treatments: this.fb.nonNullable.group({
      plan: ['', [Validators.required]],
      procedures: ['', [Validators.required]]
    }),
    medios: this.fb.nonNullable.group({
      imagenes: ['', [Validators.required]],
      notas: ['', [Validators.required]]
    })
  });

  ngOnInit() {
    this.isProfessorEditor.set(this.route.snapshot.data['editor'] === 'professor' && this.auth.getRole() === 'Profesor');
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.service.getById(id).subscribe({
      next: history => {
        this.history.set(history);
        this.form.patchValue(history.data as any);
        this.mediaAssets.set(this.extractMediaAssets(history.data));
        if ((history.data as any)?.odontogram?.teeth) {
          this.odontogram.set(this.normalizeOdontogram((history.data as any).odontogram as OdontogramState));
        }
        if (this.readonly()) {
          this.form.disable();
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  saveDraft() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const data = this.buildPayload();
    const existing = this.history();

    let request$;
    if (existing) {
      request$ = this.service.update(existing.id, data);
    } else {
      request$ = this.service.create(data);
    }

    request$.subscribe({
      next: history => {
        this.history.set(history);
        this.form.markAsPristine();
        this.saving.set(false);
        if (!existing) {
          this.router.navigate(['/student/histories', history.id]);
        }
      },
      error: () => {
        this.saving.set(false);
      }
    });
  }

  submit() {
    if (this.isProfessorEditor()) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const data = this.buildPayload();
    const existing = this.history();

    const request$ = existing
      ? this.service.update(existing.id, data).pipe(
          switchMap(updated => this.service.submit(updated.id))
        )
      : this.service.create(data).pipe(
          switchMap(created => this.service.submit(created.id))
        );

    request$.subscribe({
      next: history => {
        this.history.set(history);
        this.form.disable();
        this.saving.set(false);
      },
      error: () => {
        this.saving.set(false);
      }
    });
  }

  setTab(tab: 'personal' | 'consulta' | 'odontograma' | 'indicadores' | 'tratamientos' | 'medios') {
    this.activeTab.set(tab);
  }

  selectMarker(marker: OdontogramMarker) {
    this.marker.set(marker);
  }

  updateOdontogram(state: Odontogram3DState) {
    this.odontogram.set(state);
  }

  onEstomatognaticoChange(field: string, event: Event) {
    const checkbox = event.target as HTMLInputElement;
    if (checkbox.checked) {
      this.estomatognaticoSelected.set(field);
    } else {
      // Limpiar el detalle cuando se desmarca
      this.form.get(`consultation.estomatognaticoDetails.${field}`)?.setValue('');
      this.updateObservationsFromEstomatognatico();
    }
  }

  saveEstomatognaticoDetail() {
    this.updateObservationsFromEstomatognatico();
    this.estomatognaticoSelected.set(null);
  }

  cancelEstomatognaticoDetail() {
    const field = this.estomatognaticoSelected();
    if (field) {
      this.form.get(`consultation.estomatognatico.${field}`)?.setValue(false);
      this.form.get(`consultation.estomatognaticoDetails.${field}`)?.setValue('');
    }
    this.estomatognaticoSelected.set(null);
  }

  private updateObservationsFromEstomatognatico() {
    const estomatognatico = this.form.get('consultation.estomatognatico')?.value as any;
    const details = this.form.get('consultation.estomatognaticoDetails')?.value as any;
    
    const labels: Record<string, string> = {
      labios: 'Labios',
      mejillas: 'Mejillas',
      maxilarSuperior: 'Maxilar superior',
      maxilarInferior: 'Maxilar inferior',
      lengua: 'Lengua',
      paladar: 'Paladar',
      piso: 'Piso',
      carrillos: 'Carrillos',
      glandulasSalivales: 'Glándulas salivales',
      orofaringe: 'Oro faringe',
      atm: 'A.T.M.',
      ganglios: 'Ganglios'
    };

    const observations: string[] = [];
    Object.keys(estomatognatico).forEach(key => {
      if (estomatognatico[key] && details[key]) {
        observations.push(`${labels[key]}: ${details[key]}`);
      }
    });

    const currentNotes = this.form.get('consultation.notes')?.value || '';
    const notesLines = currentNotes.split('\n').filter((line: string) => {
      // Filtrar líneas que no sean de estomatognático
      return !Object.values(labels).some(label => line.startsWith(`${label}:`));
    });

    if (observations.length > 0) {
      const newNotes = [...notesLines, '', '--- Examen estomatognático ---', ...observations].join('\n');
      this.form.get('consultation.notes')?.setValue(newNotes);
    } else {
      this.form.get('consultation.notes')?.setValue(notesLines.join('\n'));
    }
  }

  suggestNotes() {
    const consultation = this.form.get('consultation')?.value as any;
    const treatments = this.form.get('treatments')?.value as any;
    this.suggesting.set(true);
    this.ai.suggestNotes({
      reason: consultation?.reason,
      currentIssue: consultation?.currentIssue,
      notes: consultation?.notes,
      plan: treatments?.plan,
      procedures: treatments?.procedures
    }).subscribe({
      next: response => {
        this.form.get('consultation.notes')?.setValue(response.suggestion);
        this.suggesting.set(false);
      },
      error: () => {
        this.suggesting.set(false);
      }
    });
  }

  onMediaSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }
    const files = Array.from(input.files);
    input.value = '';
    this.uploadMediaFiles(files);
  }

  private buildPayload(): Record<string, unknown> {
    const formValue = this.form.getRawValue();
    const medios = {
      ...(formValue as any).medios,
      assets: this.mediaAssets()
    };
    return {
      ...formValue,
      medios,
      odontogram: this.odontogram()
    };
  }

  private buildDefaultTeeth(): Odontogram3DState['teeth'] {
    const surfaces: OdontogramSurface[] = ['vestibular', 'mesial', 'distal', 'occlusal', 'lingual'];
    const codes = [
      18, 17, 16, 15, 14, 13, 12, 11,
      21, 22, 23, 24, 25, 26, 27, 28,
      55, 54, 53, 52, 51,
      61, 62, 63, 64, 65,
      48, 47, 46, 45, 44, 43, 42, 41,
      31, 32, 33, 34, 35, 36, 37, 38,
      85, 84, 83, 82, 81,
      71, 72, 73, 74, 75
    ];
    const teeth: Odontogram3DState['teeth'] = {};
    for (const code of codes) {
      teeth[code] = surfaces.reduce((acc, surface) => {
        acc[surface] = 'none';
        return acc;
      }, {} as Record<OdontogramSurface, OdontogramMarker>);
    }
    return teeth;
  }

  private buildDefaultNeckLevels(): Odontogram3DState['neckLevels'] {
    const codes = [
      18, 17, 16, 15, 14, 13, 12, 11,
      21, 22, 23, 24, 25, 26, 27, 28,
      55, 54, 53, 52, 51,
      61, 62, 63, 64, 65,
      48, 47, 46, 45, 44, 43, 42, 41,
      31, 32, 33, 34, 35, 36, 37, 38,
      85, 84, 83, 82, 81,
      71, 72, 73, 74, 75
    ];
    const levels: Odontogram3DState['neckLevels'] = {};
    for (const code of codes) {
      levels[code] = { gingiva: 1, bone: 1 };
    }
    return levels;
  }

  private normalizeOdontogram(state: OdontogramState): OdontogramState {
    const normalized: OdontogramState = {
      teeth: state.teeth ?? this.buildDefaultTeeth(),
      depths: state.depths ?? {},
      neckLevels: state.neckLevels ?? this.buildDefaultNeckLevels(),
      showNumbers: state.showNumbers ?? true,
      showArchLabels: state.showArchLabels ?? true
    };

    if (!state.neckLevels) {
      normalized.neckLevels = this.buildDefaultNeckLevels();
    }
    return normalized;
  }

  private extractMediaAssets(data: Record<string, unknown>): MediaAsset[] {
    const medios = (data as any)?.medios;
    const assets = medios?.assets;
    if (!Array.isArray(assets)) {
      return [];
    }
    return assets
      .filter(item => item && item.url)
      .map(item => ({
        id: item.id ?? crypto.randomUUID(),
        fileName: item.fileName ?? 'archivo',
        url: item.url,
        contentType: item.contentType ?? 'application/octet-stream',
        uploadedAt: item.uploadedAt ?? new Date().toISOString()
      }));
  }

  isImage(asset: MediaAsset) {
    return asset.contentType?.startsWith('image/');
  }

  isVideo(asset: MediaAsset) {
    return asset.contentType?.startsWith('video/');
  }

  private uploadMediaFiles(files: File[]) {
    if (files.length === 0) {
      return;
    }

    this.uploadingMedia.set(true);
    this.ensureHistory().subscribe({
      next: (history: ClinicalHistory | null) => {
        if (!history) {
          this.uploadingMedia.set(false);
          return;
        }
        const queue = [...files];
        const uploadNext = () => {
          const file = queue.shift();
          if (!file) {
            this.uploadingMedia.set(false);
            return;
          }
          this.service.uploadMedia(history.id, file).subscribe({
            next: updated => {
              this.history.set(updated);
              this.mediaAssets.set(this.extractMediaAssets(updated.data as any));
              uploadNext();
            },
            error: () => {
              this.uploadingMedia.set(false);
            }
          });
        };
        uploadNext();
      },
      error: () => {
        this.uploadingMedia.set(false);
      }
    });
  }

  private ensureHistory(): Observable<ClinicalHistory | null> {
    const existing = this.history();
    if (existing) {
      return of(existing);
    }

    if (this.isProfessorEditor()) {
      const data = this.buildPayload();
      return this.service.create(data).pipe(
        map(created => {
          this.history.set(created);
          return created;
        })
      );
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return of(null);
    }

    const data = this.buildPayload();
    return this.service.create(data).pipe(
      map(created => {
        this.history.set(created);
        return created;
      })
    );
  }
}
