import { Injectable, computed, inject, signal } from '@angular/core';
import { PostsApi } from '../../core/api/posts-api';
import { toMessage } from '../../core/api/problem-details';
import { PagedResult, PostListQuery, PostSummary } from '../../core/api/models';

const DEFAULT_QUERY: PostListQuery = { page: 1, pageSize: 10, sort: 'date', dir: 'desc' };

/**
 * Three signals — data, loading, error — are the whole state story for a screen.
 *
 * Not NgRx: there is no state shared across unrelated features, no need to replay
 * actions, and no reducer indirection worth the ceremony at this size. A service holding
 * signals is directly readable, and swapping it for a store later is a local change.
 */
@Injectable({ providedIn: 'root' })
export class PostsStore {
  private readonly api = inject(PostsApi);

  private readonly result = signal<PagedResult<PostSummary> | null>(null);

  readonly query = signal<PostListQuery>({ ...DEFAULT_QUERY });
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly posts = computed(() => this.result()?.items ?? []);
  readonly page = computed(() => this.result()?.page ?? 1);
  readonly totalPages = computed(() => this.result()?.totalPages ?? 0);
  readonly totalCount = computed(() => this.result()?.totalCount ?? 0);
  readonly isEmpty = computed(() => !this.loading() && this.posts().length === 0);

  /** Applying a filter returns to page 1: staying on page 4 of a narrower result is rarely what was meant. */
  applyFilters(patch: Partial<PostListQuery>): void {
    this.query.update((current) => ({ ...current, ...patch, page: 1 }));
    this.refresh();
  }

  goToPage(page: number): void {
    this.query.update((current) => ({ ...current, page }));
    this.refresh();
  }

  reset(): void {
    this.query.set({ ...DEFAULT_QUERY });
    this.refresh();
  }

  refresh(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.list(this.query()).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(toMessage(err));
        this.loading.set(false);
      },
    });
  }
}
