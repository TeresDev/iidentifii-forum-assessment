import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthApi } from '../api/auth-api';
import { AuthResponse, UserSummary } from '../api/models';

const STORAGE_KEY = 'forum.session';

interface Session {
  accessToken: string;
  expiresAtUtc: string;
  user: UserSummary;
}

/**
 * The session is kept in localStorage so a refresh does not log you out.
 *
 * Trade-off, stated plainly: localStorage is readable by any JavaScript running on the
 * page, so a successful XSS can exfiltrate the token. The safer arrangement is a
 * short-lived token held only in memory alongside an HttpOnly refresh cookie, which
 * survives refresh without exposing the token to script. That was not built here — see
 * the README's limitations section.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);

  private readonly session = signal<Session | null>(restore());

  readonly user = computed(() => this.session()?.user ?? null);
  readonly username = computed(() => this.user()?.username ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly isModerator = computed(() => this.user()?.role === 'Moderator');

  /** Read by the interceptor on every request. */
  token(): string | null {
    const current = this.session();

    if (current && hasExpired(current)) {
      this.clear();
      return null;
    }

    return current?.accessToken ?? null;
  }

  login(username: string, password: string): Observable<AuthResponse> {
    return this.api.login(username, password).pipe(tap((response) => this.store(response)));
  }

  register(username: string, password: string): Observable<UserSummary> {
    return this.api.register(username, password);
  }

  logout(): void {
    this.clear();
  }

  private store(response: AuthResponse): void {
    const session: Session = {
      accessToken: response.accessToken,
      expiresAtUtc: response.expiresAtUtc,
      user: response.user,
    };

    localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    this.session.set(session);
  }

  private clear(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
  }
}

/**
 * Expiry is checked on load as well as on use. Without it a stale token is replayed after
 * a refresh and every request fails with a 401 that looks like a bug rather than a
 * finished session.
 */
function restore(): Session | null {
  const raw = localStorage.getItem(STORAGE_KEY);

  if (!raw) {
    return null;
  }

  try {
    const session = JSON.parse(raw) as Session;

    if (!session.accessToken || !session.user || hasExpired(session)) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }

    return session;
  } catch {
    localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

function hasExpired(session: Session): boolean {
  return new Date(session.expiresAtUtc).getTime() <= Date.now();
}
