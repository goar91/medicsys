import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, NgFor, NgIf } from '@angular/common';
import { AgendaService, Appointment, AvailabilityResponse, TimeSlot, UserSummary } from '../../core/agenda.service';
import { AuthService } from '../../core/auth.service';
import { TopNavComponent } from '../../shared/top-nav/top-nav';

interface CalendarDay {
  date: Date | null;
  label: string;
  isToday: boolean;
}

@Component({
  selector: 'app-agenda',
  standalone: true,
  imports: [NgIf, NgFor, DatePipe, TopNavComponent],
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
  readonly students = signal<UserSummary[]>([]);
  readonly selectedProfessorId = signal<string>('');
  readonly selectedStudentId = signal<string>('');
  readonly appointmentReason = signal('Consulta odontológica');
  readonly patientName = signal('');

  readonly professorAvailability = signal<AvailabilityResponse | null>(null);
  readonly studentAvailability = signal<AvailabilityResponse | null>(null);
  readonly appointments = signal<Appointment[]>([]);
  readonly reminders = signal<{ id: string; message: string; channel: string; status: string; target: string; scheduledAt: string }[]>([]);
  readonly creating = signal(false);

  readonly calendarDays = computed(() => this.buildCalendar(this.selectedDate()));

  ngOnInit() {
    this.loadUsers();
    this.loadReminders();
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

  setProfessor(id: string) {
    this.selectedProfessorId.set(id);
    this.refreshAvailability();
  }

  setStudent(id: string) {
    this.selectedStudentId.set(id);
    const selected = this.students().find(student => student.id === id);
    if (selected) {
      this.patientName.set(selected.fullName);
    }
    this.refreshAvailability();
  }

  book(slot: TimeSlot) {
    const professorId = this.selectedProfessorId();
    const studentId = this.selectedStudentId();
    if (!professorId || !studentId) return;

    this.creating.set(true);
    const reason = this.appointmentReason().trim() || 'Consulta odontológica';
    const patientName = this.isProvider()
      ? (this.patientName().trim() || this.getSelectedStudentName())
      : (this.auth.user()?.fullName ?? 'Paciente');

    this.agenda.createAppointment({
      studentId,
      professorId,
      patientName,
      reason,
      startAt: slot.startAt,
      endAt: slot.endAt
    }).subscribe({
      next: () => {
        this.creating.set(false);
        this.loadAppointments();
        this.refreshAvailability();
      },
      error: () => {
        this.creating.set(false);
      }
    });
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

      this.agenda.getStudents().subscribe({
        next: students => {
          this.students.set(students);
          if (!this.selectedStudentId() && students.length > 0) {
            this.selectedStudentId.set(students[0].id);
            this.patientName.set(students[0].fullName);
          }
          this.refreshAvailability();
        }
      });
      return;
    }

    this.agenda.getProfessors().subscribe({
      next: professors => {
        this.professors.set(professors);
        if (!this.selectedProfessorId() && professors.length > 0) {
          this.selectedProfessorId.set(professors[0].id);
        }
        this.selectedStudentId.set(this.currentUserId());
        this.refreshAvailability();
      }
    });
  }

  private loadAppointments() {
    const params: { studentId?: string; professorId?: string } = {};
    if (this.isProvider()) {
      if (this.selectedStudentId()) params.studentId = this.selectedStudentId();
    } else {
      params.studentId = this.currentUserId();
    }
    if (this.selectedProfessorId()) params.professorId = this.selectedProfessorId();

    this.agenda.getAppointments(params).subscribe({
      next: items => this.appointments.set(items)
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
    const studentId = this.isProvider() ? this.selectedStudentId() : this.currentUserId();

    if (professorId) {
      this.agenda.getAvailability(date, { professorId }).subscribe({
        next: availability => this.professorAvailability.set(availability)
      });
    }
    if (studentId) {
      this.agenda.getAvailability(date, { studentId }).subscribe({
        next: availability => this.studentAvailability.set(availability)
      });
    }
    this.loadAppointments();
  }

  private getSelectedStudentName() {
    const student = this.students().find(item => item.id === this.selectedStudentId());
    return student?.fullName ?? 'Paciente';
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
