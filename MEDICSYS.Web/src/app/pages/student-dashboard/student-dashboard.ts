import { Component, OnInit, computed, signal } from '@angular/core';
import { NgFor, NgIf, DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { TopNavComponent } from '../../shared/top-nav/top-nav';
import { ClinicalHistoryService } from '../../core/clinical-history.service';
import { ClinicalHistory } from '../../core/models';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [TopNavComponent, NgFor, NgIf, DatePipe, RouterLink],
  templateUrl: './student-dashboard.html',
  styleUrl: './student-dashboard.scss'
})
export class StudentDashboardComponent implements OnInit {
  readonly histories = signal<ClinicalHistory[]>([]);
  readonly loading = signal(true);
  readonly draftCount = computed(() =>
    this.histories().filter(history => history.status === 'Draft').length
  );
  readonly submittedCount = computed(() =>
    this.histories().filter(history => history.status === 'Submitted').length
  );

  constructor(
    private readonly service: ClinicalHistoryService,
    private readonly router: Router
  ) {}

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

  newHistory() {
    this.router.navigate(['/student/histories/new']);
  }
}
