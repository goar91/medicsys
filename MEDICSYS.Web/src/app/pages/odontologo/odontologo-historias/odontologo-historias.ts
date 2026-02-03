import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ClinicalHistoryService } from '../../../core/clinical-history.service';
import { ClinicalHistory } from '../../../core/models';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';

@Component({
  selector: 'app-odontologo-historias',
  standalone: true,
  imports: [CommonModule, FormsModule, TopNavComponent],
  templateUrl: './odontologo-historias.html',
  styleUrl: './odontologo-historias.scss'
})
export class OdontologoHistoriasComponent implements OnInit {
  readonly histories = signal<ClinicalHistory[]>([]);
  readonly loading = signal(true);
  readonly searchTerm = signal('');

  readonly filteredHistories = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) {
      return this.histories();
    }

    return this.histories().filter(h => {
      const data = h.data as any;
      const personal = data?.personal || {};
      
      const fullName = `${personal.firstName || ''} ${personal.lastName || ''}`.toLowerCase();
      const idNumber = (personal.idNumber || '').toLowerCase();
      const clinicalHistoryNumber = (personal.clinicalHistoryNumber || '').toLowerCase();

      return fullName.includes(term) || 
             idNumber.includes(term) || 
             clinicalHistoryNumber.includes(term);
    });
  });

  constructor(
    private readonly service: ClinicalHistoryService,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.route.queryParamMap.subscribe(params => {
      const idNumber = params.get('idNumber');
      if (idNumber) {
        this.searchTerm.set(idNumber);
      }
    });
    this.loadHistories();
  }

  loadHistories() {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: histories => {
        this.histories.set(histories);
        this.loading.set(false);
      },
      error: err => {
        console.error('Error loading histories:', err);
        this.loading.set(false);
        alert('Error al cargar las historias clínicas. Por favor intenta nuevamente.');
      }
    });
  }

  onSearch(event: Event) {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
  }

  createNew() {
    this.router.navigate(['/odontologo/histories/new']);
  }

  editHistory(history: ClinicalHistory) {
    this.router.navigate(['/odontologo/histories', history.id]);
  }

  deleteHistory(history: ClinicalHistory) {
    if (!confirm(`¿Estás seguro de eliminar la historia clínica de ${this.getPatientName(history)}?`)) {
      return;
    }

    this.service.delete(history.id).subscribe({
      next: () => {
        this.loadHistories();
      },
      error: err => {
        console.error('Error deleting history:', err);
        alert('Error al eliminar la historia clínica.');
      }
    });
  }

  getPatientName(history: ClinicalHistory): string {
    const data = history.data as any;
    const personal = data?.personal || {};
    return `${personal.firstName || ''} ${personal.lastName || ''}`.trim() || 'Sin nombre';
  }

  getPatientId(history: ClinicalHistory): string {
    const data = history.data as any;
    return data?.personal?.idNumber || 'N/A';
  }

  getClinicalHistoryNumber(history: ClinicalHistory): string {
    const data = history.data as any;
    return data?.personal?.clinicalHistoryNumber || 'N/A';
  }

  getStatusText(status: string): string {
    const statusMap: Record<string, string> = {
      'Draft': 'Borrador',
      'Submitted': 'Enviada',
      'Approved': 'Aprobada',
      'Rejected': 'Rechazada'
    };
    return statusMap[status] || status;
  }

  getStatusClass(status: string): string {
    const classMap: Record<string, string> = {
      'Draft': 'status-draft',
      'Submitted': 'status-submitted',
      'Approved': 'status-approved',
      'Rejected': 'status-rejected'
    };
    return classMap[status] || '';
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('es-EC', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}
