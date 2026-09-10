import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './api-config';
import { LikeCountResponse } from './models';

@Injectable({ providedIn: 'root' })
export class LikesApi {
  private readonly http = inject(HttpClient);

  like(postId: string): Observable<LikeCountResponse> {
    return this.http.post<LikeCountResponse>(`${API_BASE}/posts/${postId}/likes`, {});
  }

  unlike(postId: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/posts/${postId}/likes`);
  }
}
