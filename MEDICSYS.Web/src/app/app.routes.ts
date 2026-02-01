import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { roleGuard } from './core/role.guard';
import { LoginComponent } from './pages/login/login';
import { StudentDashboardComponent } from './pages/student-dashboard/student-dashboard';
import { ProfessorDashboardComponent } from './pages/professor-dashboard/professor-dashboard';
import { ClinicalHistoryFormComponent } from './pages/clinical-history-form/clinical-history-form';
import { ClinicalHistoryReviewComponent } from './pages/clinical-history-review/clinical-history-review';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent },
  {
    path: 'student',
    component: StudentDashboardComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Alumno'] }
  },
  {
    path: 'student/histories/new',
    component: ClinicalHistoryFormComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Alumno'] }
  },
  {
    path: 'student/histories/:id',
    component: ClinicalHistoryFormComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Alumno'] }
  },
  {
    path: 'professor',
    component: ProfessorDashboardComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Profesor'] }
  },
  {
    path: 'professor/histories/:id',
    component: ClinicalHistoryReviewComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Profesor'] }
  },
  { path: '**', redirectTo: 'login' }
];
