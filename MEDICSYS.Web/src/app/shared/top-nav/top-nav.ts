import { Component, computed, inject } from '@angular/core';
import { NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-top-nav',
  standalone: true,
  imports: [NgIf, RouterLink],
  templateUrl: './top-nav.html',
  styleUrl: './top-nav.scss'
})
export class TopNavComponent {
  private readonly auth = inject(AuthService);
  readonly user = this.auth.user;
  readonly homeLink = computed(() =>
    this.auth.getRole() === 'Profesor' ? '/professor' : '/student'
  );

  logout() {
    this.auth.logout();
  }
}
