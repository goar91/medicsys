import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgIf, NgFor } from '@angular/common';
import { switchMap } from 'rxjs/operators';
import { ClinicalHistoryService } from '../../core/clinical-history.service';
import { ClinicalHistory } from '../../core/models';
import { TopNavComponent } from '../../shared/top-nav/top-nav';

type OdontogramMarker = 'none' | 'missing' | 'caries' | 'filled' | 'extracted' | 'needs-treatment';

interface OdontogramTooth {
  code: number;
  marker: OdontogramMarker;
}

interface OdontogramState {
  teeth: OdontogramTooth[];
}

@Component({
  selector: 'app-clinical-history-form',
  standalone: true,
  imports: [ReactiveFormsModule, NgIf, NgFor, TopNavComponent],
  templateUrl: './clinical-history-form.html',
  styleUrl: './clinical-history-form.scss'
})
export class ClinicalHistoryFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(ClinicalHistoryService);

  readonly history = signal<ClinicalHistory | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly activeTab = signal<'personal' | 'consulta' | 'odontograma' | 'indicadores' | 'tratamientos' | 'medios'>('personal');
  readonly marker = signal<OdontogramMarker>('caries');
  readonly readonly = computed(() => {
    const status = this.history()?.status;
    return status === 'Submitted' || status === 'Approved';
  });

  readonly odontogram = signal<OdontogramState>({
    teeth: [
      18, 17, 16, 15, 14, 13, 12, 11,
      21, 22, 23, 24, 25, 26, 27, 28,
      55, 54, 53, 52, 51,
      61, 62, 63, 64, 65,
      48, 47, 46, 45, 44, 43, 42, 41,
      31, 32, 33, 34, 35, 36, 37, 38,
      85, 84, 83, 82, 81,
      71, 72, 73, 74, 75
    ].map(code => ({ code, marker: 'none' }))
  });

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
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.service.getById(id).subscribe({
      next: history => {
        this.history.set(history);
        this.form.patchValue(history.data as any);
        if ((history.data as any)?.odontogram) {
          this.odontogram.set((history.data as any).odontogram as OdontogramState);
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

    const request$ = existing
      ? this.service.update(existing.id, data)
      : this.service.create(data);

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

  toggleTooth(code: number) {
    if (this.readonly()) {
      return;
    }
    const current = this.odontogram();
    const marker = this.marker();
    const teeth = current.teeth.map(tooth => {
      if (tooth.code !== code) {
        return tooth;
      }
      return {
        ...tooth,
        marker: tooth.marker === marker ? 'none' : marker
      };
    });
    this.odontogram.set({ teeth });
  }

  private buildPayload(): Record<string, unknown> {
    return {
      ...this.form.getRawValue(),
      odontogram: this.odontogram()
    };
  }
}
