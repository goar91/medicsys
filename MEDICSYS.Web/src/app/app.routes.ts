import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { roleGuard } from './core/role.guard';
import { LoginComponent } from './pages/login/login';
import { RegisterComponent } from './pages/register/register';
import { StudentDashboardComponent } from './pages/student-dashboard/student-dashboard';
import { ProfessorDashboardComponent } from './pages/professor-dashboard/professor-dashboard';
import { ClinicalHistoryFormComponent } from './pages/clinical-history-form/clinical-history-form';
import { ClinicalHistoryReviewComponent } from './pages/clinical-history-review/clinical-history-review';
import { AgendaComponent } from './pages/agenda/agenda';
import { OdontologoDashboardComponent } from './pages/odontologo/odontologo-dashboard/odontologo-dashboard';
import { OdontologoPacientesComponent } from './pages/odontologo/odontologo-pacientes/odontologo-pacientes';
import { OdontologoHistoriasComponent } from './pages/odontologo/odontologo-historias/odontologo-historias';
import { OdontologoFacturacionComponent } from './pages/odontologo/odontologo-facturacion/odontologo-facturacion.component';
import { OdontologoFacturaFormComponent } from './pages/odontologo/odontologo-factura-form/odontologo-factura-form';
import { OdontologoFacturaDetalleComponent } from './pages/odontologo/odontologo-factura-detalle/odontologo-factura-detalle';
import { OdontologoContabilidadComponent } from './pages/odontologo/odontologo-contabilidad/odontologo-contabilidad';
import { OdontologoInventarioComponent } from './pages/odontologo/odontologo-inventario/odontologo-inventario';
import { ProfessorPatientsFormComponent } from './pages/professor-patients-form/professor-patients-form';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
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
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/pacientes',
    component: OdontologoPacientesComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/historias',
    component: OdontologoHistoriasComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/agenda',
    component: AgendaComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/facturacion',
    component: OdontologoFacturacionComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/facturacion/new',
    component: OdontologoFacturaFormComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/facturacion/:id',
    component: OdontologoFacturaDetalleComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/contabilidad',
    loadComponent: () => import('./pages/odontologo/contabilidad/contabilidad-dashboard').then(m => m.ContabilidadDashboardComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/contabilidad/compras',
    loadComponent: () => import('./pages/odontologo/contabilidad/compras/compras').then(m => m.ComprasComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/contabilidad/gastos',
    loadComponent: () => import('./pages/odontologo/contabilidad/gastos/gastos.component').then(m => m.GastosComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/contabilidad/reportes',
    loadComponent: () => import('./pages/odontologo/contabilidad/reportes/reportes.component').then(m => m.ReportesComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
  },
  {
    path: 'odontologo/inventario',
    loadComponent: () => import('./pages/odontologo/inventario/kardex.component').then(m => m.KardexComponent),
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Odontologo'] }
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
    path: 'professor/dashboard',
    component: ProfessorDashboardComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Profesor'] }
  },
  {
    path: 'professor/patients/new',
    component: ProfessorPatientsFormComponent,
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Profesor'] }
  },
  {
    path: 'professor/patients/:id/edit',
    component: ProfessorPatientsFormComponent,
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
