import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { toMessage } from '../../../core/api/problem-details';
import { AuthStore } from '../../../core/auth/auth-store';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Register {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  // Mirrors the server's rules so the obvious mistakes are caught without a round trip.
  // The server validates independently — this is convenience, not the control.
  protected readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(32)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
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
