import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TopNavComponent } from '../../../shared/top-nav/top-nav';
import { OdontologoInnovationService, TelemedicineMessage, TelemedicineSession } from '../../../core/odontologo-innovation.service';

@Component({
  selector: 'app-telemedicina',
  standalone: true,
  imports: [CommonModule, FormsModule, TopNavComponent],
  templateUrl: './telemedicina.component.html',
  styleUrl: './telemedicina.component.scss'
})
export class TelemedicinaComponent implements OnInit {
  private readonly service = inject(OdontologoInnovationService);

  sessions = signal<TelemedicineSession[]>([]);
  messages = signal<TelemedicineMessage[]>([]);
  patients = signal<Array<{ id: string; fullName: string }>>([]);
  selectedSessionId = signal<string | null>(null);
  loading = signal(false);

  patientId = '';
  patientName = '';
  topic = '';
  meetingLink = '';
  scheduledStartAt = '';
  scheduledEndAt = '';
  chatMessage = '';

  ngOnInit(): void {
    this.loadPatients();
    this.loadSessions();
  }

  loadPatients(): void {
    this.service.getPortalPatients().subscribe({
      next: patients => this.patients.set(patients.map(p => ({ id: p.id, fullName: p.fullName }))),
      error: err => console.error('Error loading patients', err)
    });
  }

  loadSessions(): void {
    this.loading.set(true);
    this.service.getTelemedicineSessions().subscribe({
      next: sessions => {
        this.sessions.set(sessions);
        this.loading.set(false);
      },
      error: err => {
        console.error('Error loading telemedicine sessions', err);
        this.loading.set(false);
      }
    });
  }

  createSession(): void {
    const payload = {
      patientId: this.patientId || null,
      patientName: this.patientName,
      topic: this.topic,
      meetingLink: this.meetingLink || null,
      scheduledStartAt: this.scheduledStartAt,
      scheduledEndAt: this.scheduledEndAt
    };

    this.service.createTelemedicineSession(payload).subscribe({
      next: () => {
        this.patientId = '';
        this.patientName = '';
        this.topic = '';
        this.meetingLink = '';
        this.scheduledStartAt = '';
        this.scheduledEndAt = '';
        this.loadSessions();
      },
      error: err => console.error('Error creating session', err)
    });
  }

  openSession(sessionId: string): void {
    this.selectedSessionId.set(sessionId);
    this.service.getTelemedicineMessages(sessionId).subscribe({
      next: messages => this.messages.set(messages),
      error: err => console.error('Error loading chat', err)
    });
  }

  sendMessage(): void {
    const sessionId = this.selectedSessionId();
    const message = this.chatMessage.trim();

    if (!sessionId || !message) {
      return;
    }

    this.service.addTelemedicineMessage(sessionId, { message }).subscribe({
      next: () => {
        this.chatMessage = '';
        this.openSession(sessionId);
      },
      error: err => console.error('Error sending message', err)
    });
  }

  startSession(sessionId: string): void {
    this.service.startTelemedicineSession(sessionId).subscribe({
      next: () => this.loadSessions(),
      error: err => console.error('Error starting session', err)
    });
  }

  endSession(sessionId: string): void {
    this.service.endTelemedicineSession(sessionId).subscribe({
      next: () => this.loadSessions(),
      error: err => console.error('Error ending session', err)
    });
  }

  cancelSession(sessionId: string): void {
    this.service.cancelTelemedicineSession(sessionId).subscribe({
      next: () => this.loadSessions(),
      error: err => console.error('Error canceling session', err)
    });
  }
}
