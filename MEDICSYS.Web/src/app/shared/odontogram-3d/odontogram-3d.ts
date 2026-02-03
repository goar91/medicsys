import { Component, EventEmitter, Input, Output, signal, computed } from '@angular/core';
import { NgFor, NgIf, NgStyle } from '@angular/common';

export type OdontogramMarker = 
  | 'none' 
  // Realizados (azul)
  | 'caries-done' | 'extraction-caries-done' | 'extraction-other-done' | 'endodontics-caries-done' | 'endodontics-other-done' | 'crown-done' | 'fixed-prosthesis-done' | 'ppr-done' | 'total-prosthesis-done'
  // Por realizar (rojo)
  | 'caries-planned' | 'extraction-caries-planned' | 'extraction-other-planned' | 'endodontics-caries-planned' | 'endodontics-other-planned' | 'crown-planned' | 'fixed-prosthesis-planned' | 'ppr-planned' | 'total-prosthesis-planned';

export type OdontogramSurface = 'vestibular' | 'mesial' | 'distal' | 'occlusal' | 'lingual';

export interface Odontogram3DState {
  teeth: Record<number, Record<OdontogramSurface, OdontogramMarker>>;
  depths: Record<number, number>;
  neckLevels: Record<number, { gingiva: number; bone: number }>;
  showNumbers?: boolean;
  showArchLabels?: boolean;
  prosthesisSpans?: Record<string, { start: number; end: number; type: 'fixed' | 'ppr' | 'total' }>;
}

interface ToothData {
  code: number;
  type: 'incisor' | 'canine' | 'premolar' | 'molar';
  quadrant: number;
  position: number;
  arch: 'upper' | 'lower';
}

@Component({
  selector: 'app-odontogram-3d',
  standalone: true,
  imports: [NgFor, NgIf, NgStyle],
  templateUrl: './odontogram-3d.html',
  styleUrl: './odontogram-3d.scss'
})
export class Odontogram3DComponent {
  @Input({ required: true }) state!: Odontogram3DState;
  @Input({ required: true }) activeMarker!: OdontogramMarker;
  @Input() readonly = false;
  @Output() stateChange = new EventEmitter<Odontogram3DState>();

  readonly showNumbers = signal(true);
  readonly selectedTooth = signal<number | null>(null);
  readonly hoveredTooth = signal<number | null>(null);
  readonly rangeSelectionMode = signal(false);
  readonly rangeStart = signal<number | null>(null);
  readonly rangeEnd = signal<number | null>(null);
  readonly showArchSelector = signal(false);

  readonly upperTeeth: ToothData[] = [
    ...this.createQuadrant(1, 'upper'),
    ...this.createQuadrant(2, 'upper')
  ];

  readonly lowerTeeth: ToothData[] = [
    ...this.createQuadrant(4, 'lower'),
    ...this.createQuadrant(3, 'lower')
  ];

  private readonly markerStyles: Record<OdontogramMarker, { bg: string; border: string; icon?: string; color?: string }> = {
    none: { bg: 'transparent', border: 'var(--odonto-border, #cbd5e1)' },
    
    // Tratamientos realizados (azul)
    'caries-done': { bg: 'var(--odonto-done-light, #dbeafe)', border: 'var(--odonto-done, #3b82f6)', color: 'var(--odonto-done, #3b82f6)' },
    'extraction-caries-done': { bg: 'var(--odonto-done-light, #dbeafe)', border: 'var(--odonto-done, #3b82f6)', icon: '✕', color: 'var(--odonto-done, #3b82f6)' },
    'extraction-other-done': { bg: 'var(--odonto-done-light, #dbeafe)', border: 'var(--odonto-done, #3b82f6)', icon: '⊗', color: 'var(--odonto-done, #3b82f6)' },
    'endodontics-caries-done': { bg: 'var(--odonto-done-light, #dbeafe)', border: 'var(--odonto-done, #3b82f6)', icon: '▲', color: 'var(--odonto-done, #3b82f6)' },
    'endodontics-other-done': { bg: 'var(--odonto-done-light, #dbeafe)', border: 'var(--odonto-done, #3b82f6)', icon: '⊚', color: 'var(--odonto-done, #3b82f6)' },
    'crown-done': { bg: 'var(--odonto-done-light, #dbeafe)', border: 'var(--odonto-done, #3b82f6)', icon: '■', color: 'var(--odonto-done, #3b82f6)' },
    'fixed-prosthesis-done': { bg: 'var(--odonto-done-light, #dbeafe)', border: 'var(--odonto-done, #3b82f6)', icon: '●', color: 'var(--odonto-done, #3b82f6)' },
    'ppr-done': { bg: 'var(--odonto-done-light, #dbeafe)', border: 'var(--odonto-done, #3b82f6)', icon: '⟮⟯', color: 'var(--odonto-done, #3b82f6)' },
    'total-prosthesis-done': { bg: 'var(--odonto-done-light, #dbeafe)', border: 'var(--odonto-done, #3b82f6)', icon: '━', color: 'var(--odonto-done, #3b82f6)' },
    
    // Tratamientos por realizar (rojo)
    'caries-planned': { bg: 'var(--odonto-planned-light, #fee2e2)', border: 'var(--odonto-planned, #ef4444)', color: 'var(--odonto-planned, #ef4444)' },
    'extraction-caries-planned': { bg: 'var(--odonto-planned-light, #fee2e2)', border: 'var(--odonto-planned, #ef4444)', icon: '✕', color: 'var(--odonto-planned, #ef4444)' },
    'extraction-other-planned': { bg: 'var(--odonto-planned-light, #fee2e2)', border: 'var(--odonto-planned, #ef4444)', icon: '⊗', color: 'var(--odonto-planned, #ef4444)' },
    'endodontics-caries-planned': { bg: 'var(--odonto-planned-light, #fee2e2)', border: 'var(--odonto-planned, #ef4444)', icon: '▲', color: 'var(--odonto-planned, #ef4444)' },
    'endodontics-other-planned': { bg: 'var(--odonto-planned-light, #fee2e2)', border: 'var(--odonto-planned, #ef4444)', icon: '⊚', color: 'var(--odonto-planned, #ef4444)' },
    'crown-planned': { bg: 'var(--odonto-planned-light, #fee2e2)', border: 'var(--odonto-planned, #ef4444)', icon: '■', color: 'var(--odonto-planned, #ef4444)' },
    'fixed-prosthesis-planned': { bg: 'var(--odonto-planned-light, #fee2e2)', border: 'var(--odonto-planned, #ef4444)', icon: '●', color: 'var(--odonto-planned, #ef4444)' },
    'ppr-planned': { bg: 'var(--odonto-planned-light, #fee2e2)', border: 'var(--odonto-planned, #ef4444)', icon: '⟮⟯', color: 'var(--odonto-planned, #ef4444)' },
    'total-prosthesis-planned': { bg: 'var(--odonto-planned-light, #fee2e2)', border: 'var(--odonto-planned, #ef4444)', icon: '━', color: 'var(--odonto-planned, #ef4444)' }
  };

  private createQuadrant(quadrant: number, arch: 'upper' | 'lower'): ToothData[] {
    const teeth: ToothData[] = [];
    const start = quadrant === 1 || quadrant === 4 ? 8 : 1;
    const end = quadrant === 1 || quadrant === 4 ? 1 : 8;
    const step = quadrant === 1 || quadrant === 4 ? -1 : 1;
    
    for (let i = start; quadrant === 1 || quadrant === 4 ? i >= end : i <= end; i += step) {
      const code = quadrant * 10 + i;
      teeth.push({
        code,
        type: this.getToothType(i),
        quadrant,
        position: i,
        arch
      });
    }
    return teeth;
  }

  private getToothType(position: number): 'incisor' | 'canine' | 'premolar' | 'molar' {
    if (position <= 2) return 'incisor';
    if (position === 3) return 'canine';
    if (position <= 5) return 'premolar';
    return 'molar';
  }

  getToothStatus(code: number): OdontogramMarker {
    const surfaces = this.state.teeth[code];
    if (!surfaces) return 'none';
    
    // Si cualquier superficie tiene marcador, mostramos el más prominente
    const markers = Object.values(surfaces).filter(m => m !== 'none');
    if (markers.length === 0) return 'none';
    
    // Prioridad: extracciones > endodoncias > coronas > prótesis > caries
    const priority: Array<Exclude<OdontogramMarker, 'none'>> = [
      'extraction-caries-done', 'extraction-other-done', 'extraction-caries-planned', 'extraction-other-planned',
      'endodontics-caries-done', 'endodontics-other-done', 'endodontics-caries-planned', 'endodontics-other-planned',
      'crown-done', 'crown-planned',
      'fixed-prosthesis-done', 'fixed-prosthesis-planned',
      'ppr-done', 'ppr-planned',
      'total-prosthesis-done', 'total-prosthesis-planned',
      'caries-done', 'caries-planned'
    ];
    
    for (const marker of priority) {
      if (markers.some(m => m === marker)) return marker;
    }
    
    return markers[0];
  }

  getSurfaceMarker(code: number, surface: OdontogramSurface): OdontogramMarker {
    return this.state.teeth[code]?.[surface] || 'none';
  }

  getToothStyle(code: number): { [key: string]: string } {
    const status = this.getToothStatus(code);
    const style = this.markerStyles[status];
    return {
      '--tooth-bg': style.bg,
      '--tooth-border': style.border
    };
  }

  getSurfaceStyle(code: number, surface: OdontogramSurface): { [key: string]: string } {
    const marker = this.getSurfaceMarker(code, surface);
    const style = this.markerStyles[marker];
    const isCaries = marker === 'caries-done' || marker === 'caries-planned';
    
    return {
      'background-color': marker === 'none' ? 'transparent' : (isCaries ? style.border : style.bg),
      'border-color': style.border,
      'color': style.color || style.border,
      'opacity': isCaries ? '0.8' : '1'
    };
  }

  onToothClick(tooth: ToothData, event?: MouseEvent) {
    if (this.readonly) return;

    if (event) {
      // Click en superficie específica
      const target = event.target as HTMLElement;
      const surfaceEl = target.closest('[data-surface]') as HTMLElement;
      if (surfaceEl) {
        const surface = surfaceEl.dataset['surface'] as OdontogramSurface;
        this.toggleSurface(tooth.code, surface);
        return;
      }
    }

    // Click general en el diente
    this.selectedTooth.set(tooth.code);
  }

  toggleSurface(code: number, surface: OdontogramSurface) {
    if (this.readonly) return;
    
    const current = this.getSurfaceMarker(code, surface);
    const next = current === this.activeMarker ? 'none' : this.activeMarker;
    
    if (!this.state.teeth[code]) {
      this.state.teeth[code] = {
        vestibular: 'none',
        mesial: 'none',
        distal: 'none',
        occlusal: 'none',
        lingual: 'none'
      };
    }
    
    this.state.teeth[code][surface] = next;
    this.stateChange.emit(this.state);
  }

  toggleAllSurfaces(code: number) {
    if (this.readonly) return;
    
    const surfaces: OdontogramSurface[] = ['vestibular', 'mesial', 'distal', 'occlusal', 'lingual'];
    const hasAnyActive = surfaces.some(s => this.getSurfaceMarker(code, s) === this.activeMarker);
    
    surfaces.forEach(surface => {
      this.state.teeth[code][surface] = hasAnyActive ? 'none' : this.activeMarker;
    });
    
    this.stateChange.emit(this.state);
  }

  toggleNumbers() {
    this.showNumbers.update(v => !v);
    this.state.showNumbers = this.showNumbers();
    this.stateChange.emit(this.state);
  }

  getToothIcon(code: number): string {
    const status = this.getToothStatus(code);
    return this.markerStyles[status].icon || '';
  }

  onToothHover(code: number | null) {
    this.hoveredTooth.set(code);
  }

  // Seleccionar maxilar completo para prótesis total
  selectCompleteArch(arch: 'upper' | 'lower') {
    const teeth = arch === 'upper' ? this.upperTeeth : this.lowerTeeth;
    const surfaces: OdontogramSurface[] = ['vestibular', 'mesial', 'distal', 'occlusal', 'lingual'];
    
    teeth.forEach(tooth => {
      if (!this.state.teeth[tooth.code]) {
        this.state.teeth[tooth.code] = {
          vestibular: 'none',
          mesial: 'none',
          distal: 'none',
          occlusal: 'none',
          lingual: 'none'
        };
      }
      surfaces.forEach(surface => {
        this.state.teeth[tooth.code][surface] = this.activeMarker;
      });
    });
    
    this.showArchSelector.set(false);
    this.stateChange.emit(this.state);
  }

  // Iniciar selección de rango para PPR
  startRangeSelection() {
    this.rangeSelectionMode.set(true);
    this.rangeStart.set(null);
    this.rangeEnd.set(null);
  }

  // Seleccionar diente en modo rango
  selectToothInRange(code: number) {
    if (!this.rangeSelectionMode()) return;

    if (this.rangeStart() === null) {
      this.rangeStart.set(code);
    } else if (this.rangeEnd() === null) {
      this.rangeEnd.set(code);
      this.applyRangeMarker();
    }
  }

  // Aplicar marcador al rango seleccionado
  applyRangeMarker() {
    const start = this.rangeStart();
    const end = this.rangeEnd();
    
    if (start === null || end === null) return;

    const startQuadrant = Math.floor(start / 10);
    const endQuadrant = Math.floor(end / 10);

    // Solo permitir rangos en el mismo maxilar
    if ((startQuadrant === 1 || startQuadrant === 2) !== (endQuadrant === 1 || endQuadrant === 2)) {
      alert('El rango debe estar en el mismo maxilar (superior o inferior)');
      this.cancelRangeSelection();
      return;
    }

    const allTeeth = startQuadrant <= 2 ? this.upperTeeth : this.lowerTeeth;
    const startIdx = allTeeth.findIndex(t => t.code === start);
    const endIdx = allTeeth.findIndex(t => t.code === end);

    if (startIdx === -1 || endIdx === -1) return;

    const minIdx = Math.min(startIdx, endIdx);
    const maxIdx = Math.max(startIdx, endIdx);
    const surfaces: OdontogramSurface[] = ['vestibular', 'mesial', 'distal', 'occlusal', 'lingual'];

    for (let i = minIdx; i <= maxIdx; i++) {
      const tooth = allTeeth[i];
      if (!this.state.teeth[tooth.code]) {
        this.state.teeth[tooth.code] = {
          vestibular: 'none',
          mesial: 'none',
          distal: 'none',
          occlusal: 'none',
          lingual: 'none'
        };
      }
      surfaces.forEach(surface => {
        this.state.teeth[tooth.code][surface] = this.activeMarker;
      });
    }

    this.cancelRangeSelection();
    this.stateChange.emit(this.state);
  }

  // Cancelar selección de rango
  cancelRangeSelection() {
    this.rangeSelectionMode.set(false);
    this.rangeStart.set(null);
    this.rangeEnd.set(null);
  }

  // Mostrar selector de maxilar
  toggleArchSelector() {
    if (this.activeMarker === 'total-prosthesis-done' || this.activeMarker === 'total-prosthesis-planned') {
      this.showArchSelector.update(v => !v);
    }
  }

  // Verificar si un marcador requiere selección especial
  requiresSpecialSelection(): boolean {
    return this.activeMarker === 'total-prosthesis-done' || 
           this.activeMarker === 'total-prosthesis-planned' ||
           this.activeMarker === 'ppr-done' ||
           this.activeMarker === 'ppr-planned';
  }

  // Verificar si un diente está en el rango seleccionado
  isToothInRange(code: number): boolean {
    const start = this.rangeStart();
    const end = this.rangeEnd();
    
    if (start === null) return false;
    if (end === null) return code === start;

    const startQuadrant = Math.floor(start / 10);
    const codeQuadrant = Math.floor(code / 10);

    // Solo marcar dientes del mismo maxilar
    if ((startQuadrant === 1 || startQuadrant === 2) !== (codeQuadrant === 1 || codeQuadrant === 2)) {
      return false;
    }

    const allTeeth = startQuadrant <= 2 ? this.upperTeeth : this.lowerTeeth;
    const startIdx = allTeeth.findIndex(t => t.code === start);
    const endIdx = allTeeth.findIndex(t => t.code === end);
    const codeIdx = allTeeth.findIndex(t => t.code === code);

    if (startIdx === -1 || endIdx === -1 || codeIdx === -1) return false;

    const minIdx = Math.min(startIdx, endIdx);
    const maxIdx = Math.max(startIdx, endIdx);

    return codeIdx >= minIdx && codeIdx <= maxIdx;
  }
}
