import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './api-config';
import { TagDto } from './models';

@Injectable({ providedIn: 'root' })
export class TagsApi {
  private readonly http = inject(HttpClient);

  apply(postId: string, slug: string): Observable<TagDto> {
    return this.http.post<TagDto>(`${API_BASE}/posts/${postId}/tags`, { slug });
  }

  remove(postId: string, slug: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/posts/${postId}/tags/${slug}`);
  }
}
