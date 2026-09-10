import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PostsApi } from '../../../core/api/posts-api';
import { toMessage } from '../../../core/api/problem-details';

@Component({
  selector: 'app-post-create',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './post-create.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostCreate {
  private readonly api = inject(PostsApi);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  // Same bounds the API enforces. Client validation saves a round trip; it is not the
  // control, and the server rejects anything that gets past it.
  protected readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(200)]],
    body: ['', [Validators.required, Validators.maxLength(10000)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.api.create(this.form.getRawValue()).subscribe({
      next: (post) => this.router.navigate(['/posts', post.id]),
      error: (err) => {
        this.error.set(toMessage(err));
        this.submitting.set(false);
      },
    });
  }
}
