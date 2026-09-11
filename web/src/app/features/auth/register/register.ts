import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { toMessage } from '../../../core/api/problem-details';
import { AuthStore } from '../../../core/auth/auth-store';
import { CharCount } from '../../../shared/char-count/char-count';

const USERNAME_MIN = 3;
const USERNAME_MAX = 32;
const PASSWORD_MIN = 8;
const PASSWORD_MAX = 128;

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, CharCount],
  templateUrl: './register.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Register {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly usernameMin = USERNAME_MIN;
  protected readonly usernameMax = USERNAME_MAX;
  protected readonly passwordMin = PASSWORD_MIN;
  protected readonly passwordMax = PASSWORD_MAX;

  // Mirrors the server's rules so the obvious mistakes are caught without a round trip.
  // The server validates independently — this is convenience, not the control.
  protected readonly form = this.fb.nonNullable.group({
    username: [
      '',
      [Validators.required, Validators.minLength(USERNAME_MIN), Validators.maxLength(USERNAME_MAX)],
    ],
    password: [
      '',
      [Validators.required, Validators.minLength(PASSWORD_MIN), Validators.maxLength(PASSWORD_MAX)],
    ],
  });

  private readonly usernameValue = toSignal(this.form.controls.username.valueChanges, {
    initialValue: '',
  });
  private readonly passwordValue = toSignal(this.form.controls.password.valueChanges, {
    initialValue: '',
  });

  protected readonly usernameLength = computed(() => this.usernameValue().length);
  protected readonly passwordLength = computed(() => this.passwordValue().length);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Fix the highlighted fields before creating the account.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const { username, password } = this.form.getRawValue();

    this.auth.register(username, password).subscribe({
      next: () =>
        // Registration returns a user, not a token, so sign in to obtain one rather than
        // asking someone to type the same credentials again.
        this.auth.login(username, password).subscribe({
          next: () => this.router.navigateByUrl('/'),
          error: () => this.router.navigateByUrl('/login'),
        }),
      error: (err) => {
        this.error.set(toMessage(err));
        this.submitting.set(false);
      },
    });
  }
}
