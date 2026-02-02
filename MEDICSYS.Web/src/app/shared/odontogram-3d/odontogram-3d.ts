import { Component, EventEmitter, Input, Output, signal, computed } from '@angular/core';
import { NgFor, NgIf, NgStyle } from '@angular/common';

export type OdontogramMarker = 'none' | 'caries' | 'filled' | 'missing' | 'extracted' | 'needs-treatment' | 'crown' | 'implant' | 'root-canal';
export type OdontogramSurface = 'vestibular' | 'mesial' | 'distal' | 'occlusal' | 'lingual';

export interface Odontogram3DState {
  teeth: Record<number, Record<OdontogramSurface, OdontogramMarker>>;
  depths: Record<number, number>;
  neckLevels: Record<number, { gingiva: number; bone: number }>;
  showNumbers?: boolean;
  showArchLabels?: boolean;
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

  readonly upperTeeth: ToothData[] = [
    ...this.createQuadrant(1, 'upper'),
    ...this.createQuadrant(2, 'upper')
  ];

  readonly lowerTeeth: ToothData[] = [
    ...this.createQuadrant(4, 'lower'),
    ...this.createQuadrant(3, 'lower')
  ];

  private readonly markerStyles: Record<OdontogramMarker, { bg: string; border: string; icon?: string }> = {
    none: { bg: '#ffffff', border: '#e2e8f0' },
    caries: { bg: '#fee2e2', border: '#ef4444', icon: '●' },
    filled: { bg: '#dbeafe', border: '#3b82f6', icon: '■' },
    missing: { bg: '#f3f4f6', border: '#9ca3af', icon: '✕' },
    extracted: { bg: '#ffedd5', border: '#f97316', icon: '⊗' },
    'needs-treatment': { bg: '#fef9c3', border: '#eab308', icon: '!' },
    crown: { bg: '#e0e7ff', border: '#6366f1', icon: '♔' },
    implant: { bg: '#f3e8ff', border: '#a855f7', icon: '⚙' },
    'root-canal': { bg: '#fce7f3', border: '#ec4899', icon: '↓' }
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
    
    // Prioridad: missing > extracted > implant > crown > caries > needs-treatment > filled > root-canal
    const priority: Array<Exclude<OdontogramMarker, 'none'>> = ['missing', 'extracted', 'implant', 'crown', 'caries', 'needs-treatment', 'filled', 'root-canal'];
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
    return {
      'background-color': marker === 'none' ? 'transparent' : style.bg,
      'border-color': style.border
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
}
