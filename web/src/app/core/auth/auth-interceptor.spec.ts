import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { API_BASE } from '../api/api-config';
import { AuthStore } from './auth-store';
import { authInterceptor } from './auth-interceptor';

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;

  beforeEach(() => {
    localStorage.setItem(
      'forum.session',
      JSON.stringify({
        accessToken: 'the-token',
        expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
        user: { id: 'u1', username: 'alice', role: 'User' },
      }),
    );

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('attaches the bearer token to API requests', () => {
    client.get(`${API_BASE}/posts`).subscribe();

    const request = http.expectOne(`${API_BASE}/posts`);
    expect(request.request.headers.get('Authorization')).toBe('Bearer the-token');
    request.flush({});
  });

  // The one that matters: an interceptor without an origin check leaks the session to
  // every host the app ever calls.
  it('never sends the token to a third-party host', () => {
    client.get('https://analytics.example.com/collect').subscribe();

    const request = http.expectOne('https://analytics.example.com/collect');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('does not attach anything when signed out', () => {
    localStorage.clear();
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    const anonHttp = TestBed.inject(HttpTestingController);

    TestBed.inject(HttpClient).get(`${API_BASE}/posts`).subscribe();

    const request = anonHttp.expectOne(`${API_BASE}/posts`);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
    anonHttp.verify();
  });
});

describe('AuthStore session restore', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
  });

  function storeWith(session: unknown) {
    localStorage.setItem('forum.session', JSON.stringify(session));
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    return TestBed.inject(AuthStore);
  }

  it('restores a session that has not expired', () => {
    const auth = storeWith({
      accessToken: 'valid',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      user: { id: 'u1', username: 'alice', role: 'User' },
    });

    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.username()).toBe('alice');
  });

  // Without this an expired token is replayed after a refresh and every request fails
  // with a 401 that reads like a bug rather than a finished session.
  it('discards an expired session on load', () => {
    const auth = storeWith({
      accessToken: 'stale',
      expiresAtUtc: new Date(Date.now() - 60_000).toISOString(),
      user: { id: 'u1', username: 'alice', role: 'User' },
    });

    expect(auth.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('forum.session')).toBeNull();
  });

  it('survives corrupted storage', () => {
    localStorage.setItem('forum.session', 'not json');
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    expect(TestBed.inject(AuthStore).isAuthenticated()).toBe(false);
  });

  it('reports moderator status from the role claim', () => {
    const auth = storeWith({
      accessToken: 'valid',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      user: { id: 'u9', username: 'mod.jordan', role: 'Moderator' },
    });

    expect(auth.isModerator()).toBe(true);
  });
});
