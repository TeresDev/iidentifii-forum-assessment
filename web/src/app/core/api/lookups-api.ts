import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './api-config';
import { TagDto, UserSummary } from './models';

@Injectable({ providedIn: 'root' })
export class LookupsApi {
  private readonly http = inject(HttpClient);

  tags(): Observable<TagDto[]> {
    return this.http.get<TagDto[]>(`${API_BASE}/tags`);
  }

  users(): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(`${API_BASE}/users`);
  }
}
