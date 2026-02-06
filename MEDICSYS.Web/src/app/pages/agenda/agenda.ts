import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, NgFor, NgIf } from '@angular/common';
import { AgendaService, Appointment, AvailabilityResponse, TimeSlot, UserSummary } from '../../core/agenda.service';
import { AuthService } from '../../core/auth.service';
import { TopNavComponent } from '../../shared/top-nav/top-nav';
import { AppointmentModalComponent, AppointmentModalData } from '../../shared/appointment-modal/appointment-modal.component';

interface CalendarDay {
  date: Date | null;
  label: string;
  isToday: boolean;
}

@Component({
  selector: 'app-agenda',
  standalone: true,
  imports: [NgIf, NgFor, DatePipe, TopNavComponent, AppointmentModalComponent],
  templateUrl: './agenda.html',
  styleUrl: './agenda.scss'
})
export class AgendaComponent implements OnInit {
  private readonly agenda = inject(AgendaService);
  private readonly auth = inject(AuthService);

  readonly role = computed(() => this.auth.getRole());
  readonly isProfessor = computed(() => this.role() === 'Profesor');
  readonly isOdontologo = computed(() => this.role() === 'Odontologo');
  readonly isProvider = computed(() => this.isProfessor() || this.isOdontologo());
  readonly currentUserId = computed(() => this.auth.user()?.id ?? '');
  readonly selectedDate = signal(new Date());
  readonly professors = signal<UserSummary[]>([]);
  readonly selectedProfessorId = signal<string>('');
  readonly appointmentReason = signal('Consulta odontológica');

  readonly professorAvailability = signal<AvailabilityResponse | null>(null);
  readonly appointments = signal<Appointment[]>([]);
  readonly reminders = signal<{ id: string; message: string; channel: string; status: string; target: string; scheduledAt: string }[]>([]);
  readonly creating = signal(false);
  readonly editingAppointmentId = signal<string | null>(null);
  readonly editReason = signal('');
  readonly editPatientName = signal('');
  readonly modalData = signal<AppointmentModalData | null>(null);
  readonly selectedAppointmentForModal = signal<Appointment | null>(null);

  readonly calendarDays = computed(() => this.buildCalendar(this.selectedDate()));
  readonly appointmentsForSelectedDate = computed(() => {
    const selected = this.selectedDate();
    return this.appointments().filter(appt => {
      const apptDate = new Date(appt.startAt);
      return apptDate.toDateString() === selected.toDateString();
    });
  });

  ngOnInit() {
    this.loadUsers();
    this.loadReminders();
  }

  startEditAppointment(appt: Appointment) {
    this.editingAppointmentId.set(appt.id);
    this.editReason.set(appt.reason);
    this.editPatientName.set(appt.patientName);
  }

  cancelEdit() {
    this.editingAppointmentId.set(null);
    this.editReason.set('');
    this.editPatientName.set('');
  }

  saveEdit(appt: Appointment) {
    const newReason = this.editReason();
    const newPatientName = this.editPatientName();
    
    this.agenda.updateAppointment(appt.id, {
      reason: newReason,
      patientName: newPatientName
    }).subscribe({
      next: () => {
        this.cancelEdit();
        this.loadAppointments();
        this.refreshAvailability();
      },
      error: (err) => {
        console.error('Error al actualizar cita:', err);
        const message = err?.error?.message || err?.error || 'Error al actualizar la cita. Por favor intenta nuevamente.';
        alert(message);
      }
    });
  }

  deleteAppointment(appt: Appointment) {
    if (!confirm(`¿Estás seguro de eliminar la cita con ${appt.patientName}?`)) {
      return;
    }

    this.agenda.deleteAppointment(appt.id).subscribe({
      next: () => {
        this.loadAppointments();
        this.refreshAvailability();
      },
      error: (err) => {
        console.error('Error al eliminar cita:', err);
        const message = err?.error?.message || err?.error || 'Error al eliminar la cita. Por favor intenta nuevamente.';
        alert(message);
      }
    });
  }

  getAppointmentsForDate(date: Date): Appointment[] {
    return this.appointments().filter(appt => {
      const apptDate = new Date(appt.startAt);
      return apptDate.toDateString() === date.toDateString();
    });
  }

  calculateDuration(appt: Appointment): number {
    const start = new Date(appt.startAt);
    const end = new Date(appt.endAt);
    return Math.round((end.getTime() - start.getTime()) / (1000 * 60));
  }

  previousMonth() {
    const current = this.selectedDate();
    this.selectedDate.set(new Date(current.getFullYear(), current.getMonth() - 1, 1));
    this.refreshAvailability();
  }

  nextMonth() {
    const current = this.selectedDate();
    this.selectedDate.set(new Date(current.getFullYear(), current.getMonth() + 1, 1));
    this.refreshAvailability();
  }

  selectDay(day: CalendarDay) {
    if (!day.date) return;
    this.selectedDate.set(day.date);
    this.refreshAvailability();
  }

  onDayDoubleClick(day: CalendarDay) {
    if (!day.date) return;
    console.log('🖱️ Doble click en día:', day.date);
    this.openAppointmentModal(day.date);
  }

  openAppointmentModal(date: Date, appointment?: Appointment) {
    console.log('📋 Abriendo modal de cita');
    console.log('📅 Fecha:', date);
    console.log('✏️ Cita existente:', appointment);
    console.log('👤 Usuario actual:', this.currentUserId());
    console.log('👔 Es proveedor:', this.isProvider());
    console.log('🩺 Profesor seleccionado:', this.selectedProfessorId());
    
    const modalData: AppointmentModalData = {
      date: date,
      appointmentId: appointment?.id,
      patientName: appointment?.patientName,
      reason: appointment?.reason,
      notes: appointment?.notes || undefined,
      startAt: appointment?.startAt,
      endAt: appointment?.endAt,
      studentId: appointment?.studentId,
      professorId: appointment?.professorId || (this.isProvider() ? this.currentUserId() : (this.selectedProfessorId() || ''))
    };
    
    console.log('📤 Datos del modal:', modalData);
    this.modalData.set(modalData);
    this.selectedAppointmentForModal.set(appointment || null);
  }

  closeModal() {
    this.modalData.set(null);
    this.selectedAppointmentForModal.set(null);
  }

  saveAppointmentFromModal(payload: any) {
    console.log('💾 Guardando cita con payload:', payload);
    
    if (payload.appointmentId) {
      // Editar cita existente
      console.log('✏️ Editando cita existente:', payload.appointmentId);
      this.agenda.updateAppointment(payload.appointmentId, {
        patientName: payload.patientName,
        reason: payload.reason,
        status: payload.status,
        notes: payload.notes
      }).subscribe({
        next: () => {
          console.log('✅ Cita actualizada exitosamente');
          this.closeModal();
          this.loadAppointments();
          this.refreshAvailability();
        },
        error: (err) => {
          console.error('❌ Error al actualizar cita:', err);
          let message = 'Error al actualizar la cita. Por favor intenta nuevamente.';
          if (err?.error?.message) {
            message = err.error.message;
          } else if (typeof err?.error === 'string') {
            message = err.error;
          } else if (err?.message) {
            message = err.message;
          }
          alert(message);
        }
      });
    } else {
      // Crear nueva cita
      console.log('➕ Creando nueva cita');
      this.creating.set(true);
      this.agenda.createAppointment({
        studentId: payload.studentId,
        professorId: payload.professorId,
        patientName: payload.patientName,
        reason: payload.reason,
        startAt: payload.startAt,
        endAt: payload.endAt,
        status: payload.status,
        notes: payload.notes
      }).subscribe({
        next: (response) => {
          console.log('✅ Cita creada exitosamente:', response);
          this.creating.set(false);
          this.closeModal();
          this.loadAppointments();
          this.refreshAvailability();
        },
        error: (err) => {
          this.creating.set(false);
          console.error('❌ Error al crear cita:', err);
          let message = 'Error al crear la cita. Por favor intenta nuevamente.';
          if (err?.error?.message) {
            message = err.error.message;
          } else if (typeof err?.error === 'string') {
            message = err.error;
          } else if (err?.message) {
            message = err.message;
          }
          alert(message);
        }
      });
    }
  }

  deleteAppointmentFromModal(appointmentId: string) {
    this.agenda.deleteAppointment(appointmentId).subscribe({
      next: () => {
        this.closeModal();
        this.loadAppointments();
        this.refreshAvailability();
      },
      error: (err) => {
        console.error('Error al eliminar cita:', err);
        let message = 'Error al eliminar la cita. Por favor intenta nuevamente.';
        if (err?.error?.message) {
          message = err.error.message;
        } else if (typeof err?.error === 'string') {
          message = err.error;
        } else if (err?.message) {
          message = err.message;
        }
        alert(message);
      }
    });
  }

  onAppointmentClick(appointment: Appointment) {
    const apptDate = new Date(appointment.startAt);
    this.openAppointmentModal(apptDate, appointment);
  }

  setProfessor(id: string) {
    this.selectedProfessorId.set(id);
    this.refreshAvailability();
  }

  book(slot: TimeSlot) {
    // Ya no creamos citas desde el click en los horarios
    // Solo permitimos crear citas desde el modal de doble click
    return;
  }

  openOutlookEmail(appointment: Appointment) {
    const subject = encodeURIComponent(`Recordatorio MEDICSYS - ${appointment.patientName}`);
    const body = encodeURIComponent(`Recordatorio de cita: ${new Date(appointment.startAt).toLocaleString()}\nProfesor: ${appointment.professorName}\nMotivo: ${appointment.reason}`);
    const target = appointment.studentEmail || appointment.studentName;
    window.location.href = `mailto:${target}?subject=${subject}&body=${body}`;
  }

  openWhatsApp(appointment: Appointment) {
    const text = encodeURIComponent(`Recordatorio de cita MEDICSYS: ${new Date(appointment.startAt).toLocaleString()} con ${appointment.professorName}.`);
    const whatsappUrl = `whatsapp://send?text=${text}`;
    const webUrl = `https://wa.me/?text=${text}`;
    const opened = window.open(whatsappUrl, '_blank');
    if (!opened) {
      window.open(webUrl, '_blank');
    }
  }

  openGoogleCalendar(appointment: Appointment) {
    const start = this.formatCalendarDate(new Date(appointment.startAt));
    const end = this.formatCalendarDate(new Date(appointment.endAt));
    const text = encodeURIComponent(`Cita MEDICSYS - ${appointment.patientName}`);
    const details = encodeURIComponent(`Profesor: ${appointment.professorName}\nMotivo: ${appointment.reason}`);
    const url = `https://calendar.google.com/calendar/render?action=TEMPLATE&text=${text}&dates=${start}/${end}&details=${details}`;
    window.open(url, '_blank');
  }

  private loadUsers() {
    if (this.isProvider()) {
      this.selectedProfessorId.set(this.currentUserId());
      const providerRequest = this.isOdontologo() ? this.agenda.getOdontologos() : this.agenda.getProfessors();
      providerRequest.subscribe({
        next: providers => {
          this.professors.set(providers);
        }
      });
      this.refreshAvailability();
      return;
    }

    this.agenda.getProfessors().subscribe({
      next: professors => {
        this.professors.set(professors);
        if (!this.selectedProfessorId() && professors.length > 0) {
          this.selectedProfessorId.set(professors[0].id);
        }
        this.refreshAvailability();
      }
    });
  }

  private loadAppointments() {
    console.log('📅 Cargando citas...');
    const params: { professorId?: string } = {};
    if (this.selectedProfessorId()) params.professorId = this.selectedProfessorId();

    this.agenda.getAppointments(params).subscribe({
      next: items => {
        console.log(`✅ Citas cargadas: ${items.length}`, items);
        this.appointments.set(items);
      },
      error: (err) => {
        console.error('❌ Error al cargar citas:', err);
      }
    });
  }

  private loadReminders() {
    this.agenda.getReminders('Due').subscribe({
      next: reminders => this.reminders.set(reminders)
    });
  }

  private refreshAvailability() {
    const date = this.formatDateParam(this.selectedDate());
    const professorId = this.selectedProfessorId();

    if (professorId) {
      this.agenda.getAvailability(date, { professorId }).subscribe({
        next: availability => this.professorAvailability.set(availability)
      });
    }
    this.loadAppointments();
  }

  private formatDateParam(date: Date) {
    const pad = (value: number) => value.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
  }

  private buildCalendar(baseDate: Date): CalendarDay[] {
    const start = new Date(baseDate.getFullYear(), baseDate.getMonth(), 1);
    const end = new Date(baseDate.getFullYear(), baseDate.getMonth() + 1, 0);
    const days: CalendarDay[] = [];
    const startWeekday = start.getDay();
    for (let i = 0; i < startWeekday; i++) {
      days.push({ date: null, label: '', isToday: false });
    }
    for (let d = 1; d <= end.getDate(); d++) {
      const date = new Date(baseDate.getFullYear(), baseDate.getMonth(), d);
      const today = new Date();
      days.push({
        date,
        label: d.toString(),
        isToday: date.toDateString() === today.toDateString()
      });
    }
    return days;
  }

  private formatCalendarDate(date: Date) {
    const pad = (value: number) => value.toString().padStart(2, '0');
    return `${date.getUTCFullYear()}${pad(date.getUTCMonth() + 1)}${pad(date.getUTCDate())}T${pad(date.getUTCHours())}${pad(date.getUTCMinutes())}${pad(date.getUTCSeconds())}Z`;
  }
}
