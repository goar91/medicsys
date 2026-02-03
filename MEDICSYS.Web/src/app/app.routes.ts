import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { roleGuard } from './core/role.guard';
import { LoginComponent } from './pages/login/login';
import { StudentDashboardComponent } from './pages/student-dashboard/student-dashboard';
import { ProfessorDashboardComponent } from './pages/professor-dashboard/professor-dashboard';
import { ClinicalHistoryFormComponent } from './pages/clinical-history-form/clinical-history-form';
import { ClinicalHistoryReviewComponent } from './pages/clinical-history-review/clinical-history-review';
import { AgendaComponent } from './pages/agenda/agenda';
import { OdontologoDashboardComponent } from './pages/odontologo/odontologo-dashboard/odontologo-dashboard';
import { OdontologoPacientesComponent } from './pages/odontologo/odontologo-pacientes/odontologo-pacientes';
import { OdontologoFacturacionComponent } from './pages/odontologo/odontologo-facturacion/odontologo-facturacion.component';
import { OdontologoFacturaFormComponent } from './pages/odontologo/odontologo-factura-form/odontologo-factura-form';
import { OdontologoFacturaDetalleComponent } from './pages/odontologo/odontologo-factura-detalle/odontologo-factura-detalle';
import { OdontologoContabilidadComponent } from './pages/odontologo/odontologo-contabilidad/odontologo-contabilidad';

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
    path: 'agenda',
    component: AgendaComponent,
    canActivate: [authGuard],
  },
  // Rutas Odontólogo
  {
    path: 'odontologo/dashboard',
    component: OdontologoDashboardComponent,
    canActivate: [authGuard]
  },
  {
    path: 'odontologo/pacientes',
    component: OdontologoPacientesComponent,
    canActivate: [authGuard]
  },
  {
    path: 'odontologo/agenda',
    component: AgendaComponent,
    canActivate: [authGuard]
  },
  {
    path: 'odontologo/facturacion',
    component: OdontologoFacturacionComponent,
    canActivate: [authGuard]
  },
  {
    path: 'odontologo/facturacion/new',
    component: OdontologoFacturaFormComponent,
    canActivate: [authGuard]
  },
  {
    path: 'odontologo/facturacion/:id',
    component: OdontologoFacturaDetalleComponent,
    canActivate: [authGuard]
  },
  {
    path: 'odontologo/contabilidad',
    component: OdontologoContabilidadComponent,
    canActivate: [authGuard]
  },
  {
    path: 'odontologo/histories/new',
    component: ClinicalHistoryFormComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/histories/:id',
    component: ClinicalHistoryFormComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
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
    path: 'professor/histories/new',
    component: ClinicalHistoryFormComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Profesor'], editor: 'professor' }
  },
  {
    path: 'professor/histories/:id/edit',
    component: ClinicalHistoryFormComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Profesor'], editor: 'professor' }
  },
  {
    path: 'professor/histories/:id',
    component: ClinicalHistoryReviewComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Profesor'] }
  },
  { path: '**', redirectTo: 'login' }
];
