import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './api-config';
import {
  CreatePostRequest,
  PagedResult,
  PostDetail,
  PostListQuery,
  PostSummary,
} from './models';

@Injectable({ providedIn: 'root' })
export class PostsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/posts`;

  list(query: PostListQuery): Observable<PagedResult<PostSummary>> {
    return this.http.get<PagedResult<PostSummary>>(this.base, {
      params: toHttpParams(query),
    });
  }

  get(id: string): Observable<PostDetail> {
    return this.http.get<PostDetail>(`${this.base}/${id}`);
  }

  create(request: CreatePostRequest): Observable<PostDetail> {
    return this.http.post<PostDetail>(this.base, request);
  }
}

/**
 * Keys are the wire parameter names. Empty and undefined values are dropped rather than
 * sent blank, so clearing a filter removes it instead of sending `author=`.
 */
export function toHttpParams(query: PostListQuery): HttpParams {
  let params = new HttpParams();

  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null && value !== '') {
      params = params.set(key, String(value));
    }
  }

  return params;
}
