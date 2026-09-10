import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { PostsApi } from './posts-api';

/**
 * These assert the exact query-string the client emits.
 *
 * The API ignores unrecognised query parameters and still answers 200 with the full
 * unfiltered list. So a typo here — `authorId` for `author`, `direction` for `dir` —
 * produces a page that looks like it works while every filter silently does nothing.
 * Nothing else in the front-end would catch that, which is why these are worth the space.
 */
describe('PostsApi query parameters', () => {
  let api: PostsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    api = TestBed.inject(PostsApi);
    http = TestBed.inject(HttpTestingController);
  });

  function expectParams(actual: string, expected: Record<string, string>) {
    const params = new URLSearchParams(actual);

    for (const [key, value] of Object.entries(expected)) {
      expect(params.get(key), `expected ?${key}=${value}`).toBe(value);
    }
  }

  it('spells every list parameter the way the API reads it', () => {
    api
      .list({
        page: 2,
        pageSize: 25,
        sort: 'likes',
        dir: 'asc',
        author: '11111111-0000-0000-0000-000000000001',
        tag: 'misleading-information',
        from: '2026-03-01T00:00:00.000Z',
        to: '2026-04-01T00:00:00.000Z',
      })
      .subscribe();

    const request = http.expectOne((r) => r.url.endsWith('/posts'));

    expectParams(request.request.params.toString(), {
      page: '2',
      pageSize: '25',
      sort: 'likes',
      dir: 'asc',
      author: '11111111-0000-0000-0000-000000000001',
      tag: 'misleading-information',
      from: '2026-03-01T00:00:00.000Z',
      to: '2026-04-01T00:00:00.000Z',
    });

    request.flush({ items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 });
  });

  it('omits cleared filters rather than sending them empty', () => {
    api.list({ page: 1, pageSize: 10, author: '', tag: undefined }).subscribe();

    const request = http.expectOne((r) => r.url.endsWith('/posts'));
    const params = new URLSearchParams(request.request.params.toString());

    expect(params.has('author')).toBe(false);
    expect(params.has('tag')).toBe(false);
    expect(params.get('page')).toBe('1');

    request.flush({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 });
  });

  it('requests a single post by id', () => {
    api.get('abc').subscribe();

    http.expectOne((r) => r.url.endsWith('/posts/abc'));
  });

  afterEach(() => http.verify());
});
