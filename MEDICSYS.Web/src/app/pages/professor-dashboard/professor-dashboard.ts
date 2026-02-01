import { Component, OnInit, computed, signal } from '@angular/core';
import { NgFor, NgIf, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TopNavComponent } from '../../shared/top-nav/top-nav';
import { ClinicalHistoryService } from '../../core/clinical-history.service';
import { ClinicalHistory, ClinicalHistoryStatus } from '../../core/models';

@Component({
  selector: 'app-professor-dashboard',
  standalone: true,
  imports: [TopNavComponent, NgFor, NgIf, DatePipe, RouterLink],
  templateUrl: './professor-dashboard.html',
  styleUrl: './professor-dashboard.scss'
})
export class ProfessorDashboardComponent implements OnInit {
  readonly histories = signal<ClinicalHistory[]>([]);
  readonly loading = signal(true);
  readonly filter = signal<'all' | ClinicalHistoryStatus>('all');

  readonly filtered = computed(() => {
    const value = this.filter();
    const items = this.histories();
    if (value === 'all') {
      return items;
    }
    return items.filter(item => item.status === value);
  });

  constructor(private readonly service: ClinicalHistoryService) {}

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: items => {
        this.histories.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.histories.set([]);
        this.loading.set(false);
      }
    });
  }

  setFilter(value: 'all' | ClinicalHistoryStatus) {
    this.filter.set(value);
  }
}
