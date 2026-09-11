import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PostsApi } from '../../../core/api/posts-api';
import { toMessage } from '../../../core/api/problem-details';
import { CharCount } from '../../../shared/char-count/char-count';

const TITLE_MIN = 5;
const TITLE_MAX = 200;
const BODY_MAX = 10_000;

@Component({
  selector: 'app-post-create',
  imports: [ReactiveFormsModule, RouterLink, CharCount],
  templateUrl: './post-create.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostCreate {
  private readonly api = inject(PostsApi);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly titleMin = TITLE_MIN;
  protected readonly titleMax = TITLE_MAX;
  protected readonly bodyMax = BODY_MAX;

  // Same bounds the API enforces. Client validation saves a round trip; it is not the
  // control, and the server rejects anything that gets past it.
  protected readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(TITLE_MIN), Validators.maxLength(TITLE_MAX)]],
    body: ['', [Validators.required, Validators.maxLength(BODY_MAX)]],
  });

  private readonly titleValue = toSignal(this.form.controls.title.valueChanges, { initialValue: '' });
  private readonly bodyValue = toSignal(this.form.controls.body.valueChanges, { initialValue: '' });

  protected readonly titleLength = computed(() => this.titleValue().length);
  protected readonly bodyLength = computed(() => this.bodyValue().length);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set('Fix the highlighted fields before publishing.');
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
