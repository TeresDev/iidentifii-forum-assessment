import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './api-config';
import { AuthResponse, UserSummary } from './models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  login(username: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_BASE}/auth/login`, { username, password });
  }

  register(username: string, password: string): Observable<UserSummary> {
    return this.http.post<UserSummary>(`${API_BASE}/auth/register`, { username, password });
  }
}
