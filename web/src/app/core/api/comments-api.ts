import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from './api-config';
import { CommentDto, CreateCommentRequest, PagedResult } from './models';

@Injectable({ providedIn: 'root' })
export class CommentsApi {
  private readonly http = inject(HttpClient);

  list(postId: string, page: number, pageSize: number): Observable<PagedResult<CommentDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);

    return this.http.get<PagedResult<CommentDto>>(`${API_BASE}/posts/${postId}/comments`, { params });
  }

  create(postId: string, request: CreateCommentRequest): Observable<CommentDto> {
    return this.http.post<CommentDto>(`${API_BASE}/posts/${postId}/comments`, request);
  }
}
