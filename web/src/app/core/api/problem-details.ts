import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './models';

const MESSAGES: Record<string, string> = {
  'invalid-credentials': 'Invalid username or password.',
  'username-taken': 'That username is already taken.',
  'self-like': 'You cannot like your own post.',
  'duplicate-like': 'You have already liked this post.',
  'already-tagged': 'That tag is already applied to this post.',
  'not-found': 'That item no longer exists.',
  forbidden: 'You do not have permission to do that.',
};

/**
 * Prefers the API's stable `code` over its human-readable title, so display text can
 * change server-side without the client's messages drifting.
 */
export function toMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong.';
  }

  if (error.status === 0) {
    return 'Cannot reach the API. Is it running on http://localhost:5080?';
  }

  const problem = error.error as ProblemDetails | undefined;

  if (problem?.code && MESSAGES[problem.code]) {
    return MESSAGES[problem.code];
  }

  if (problem?.errors) {
    return Object.values(problem.errors).flat().join(' ');
  }

  return problem?.title ?? `Request failed (${error.status}).`;
}
