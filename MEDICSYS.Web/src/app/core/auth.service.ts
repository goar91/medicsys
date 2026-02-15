import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthResponse, UserProfile } from './models';
import { API_BASE_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'medicsys_token';
  private readonly expiresKey = 'medicsys_expires';
  private readonly userKey = 'medicsys_user';

  private readonly userSignal = signal<UserProfile | null>(null);
  readonly user = computed(() => this.userSignal());

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {
    this.loadFromStorage();
  }

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${API_BASE_URL}/auth/login`, { email, password });
  }

  registerStudent(payload: { email: string; password: string; fullName: string; universityId?: string | null }) {
    return this.http.post<AuthResponse>(`${API_BASE_URL}/auth/register-student`, payload);
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.expiresKey);
    localStorage.removeItem(this.userKey);
    this.userSignal.set(null);
    this.router.navigate(['/login']);
  }

  setSession(response: AuthResponse) {
    localStorage.setItem(this.tokenKey, response.token);
    localStorage.setItem(this.expiresKey, response.expiresAt);
    localStorage.setItem(this.userKey, JSON.stringify(response.user));
    this.userSignal.set(response.user);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    const expires = localStorage.getItem(this.expiresKey);

    if (!token || !expires) {
      return false;
    }

    return new Date(expires).getTime() > Date.now();
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getRole(): string {
    return this.userSignal()?.role ?? '';
  }

  private loadFromStorage() {
    const userRaw = localStorage.getItem(this.userKey);
    if (userRaw) {
      try {
        this.userSignal.set(JSON.parse(userRaw) as UserProfile);
      } catch {
        this.userSignal.set(null);
      }
    }
  }
}
