import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { CommentsApi } from '../../../core/api/comments-api';
import { LikesApi } from '../../../core/api/likes-api';
import { LookupsApi } from '../../../core/api/lookups-api';
import { CommentDto, PagedResult, PostDetail as PostDetailModel, TagDto } from '../../../core/api/models';
import { PostsApi } from '../../../core/api/posts-api';
import { toMessage } from '../../../core/api/problem-details';
import { TagsApi } from '../../../core/api/tags-api';
import { AuthStore } from '../../../core/auth/auth-store';

const COMMENT_PAGE_SIZE = 10;

@Component({
  selector: 'app-post-detail',
  imports: [RouterLink, DatePipe, ReactiveFormsModule],
  templateUrl: './post-detail.html',
  styleUrl: './post-detail.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly postsApi = inject(PostsApi);
  private readonly commentsApi = inject(CommentsApi);
  private readonly likesApi = inject(LikesApi);
  private readonly tagsApi = inject(TagsApi);
  private readonly lookups = inject(LookupsApi);
  private readonly fb = inject(FormBuilder);

  protected readonly auth = inject(AuthStore);

  protected readonly post = signal<PostDetailModel | null>(null);
  protected readonly comments = signal<PagedResult<CommentDto> | null>(null);
  protected readonly availableTags = signal<TagDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly actionError = signal<string | null>(null);
  protected readonly busy = signal(false);

  /** The API rejects this anyway; disabling the control just avoids offering a dead action. */
  protected readonly isOwnPost = computed(
    () => this.post()?.authorId === this.auth.user()?.id,
  );

  protected readonly commentForm = this.fb.nonNullable.group({
    body: ['', [Validators.required, Validators.maxLength(2000)]],
  });

  protected readonly tagForm = this.fb.nonNullable.group({
    slug: ['', Validators.required],
  });

  protected postId = '';

  ngOnInit(): void {
    this.postId = this.route.snapshot.paramMap.get('id') ?? '';
    this.load();

    if (this.auth.isModerator()) {
      this.lookups.tags().subscribe((tags) => this.availableTags.set(tags));
    }
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.postsApi.get(this.postId).subscribe({
      next: (post) => {
        this.post.set(post);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(toMessage(err));
        this.loading.set(false);
      },
    });

    this.loadComments(1);
  }

  protected loadComments(page: number): void {
    this.commentsApi.list(this.postId, Math.max(1, page), COMMENT_PAGE_SIZE).subscribe({
      next: (result) => this.comments.set(result),
      // Reported alongside the post rather than replacing it: failing to load comments
      // is no reason to hide an article that loaded fine.
      error: (err) => this.actionError.set(toMessage(err)),
    });
  }

  protected previousComments(): void {
    this.loadComments((this.comments()?.page ?? 1) - 1);
  }

  protected nextComments(): void {
    this.loadComments((this.comments()?.page ?? 1) + 1);
  }

  protected toggleLike(): void {
    const current = this.post();
    if (!current || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.actionError.set(null);

    const request: Observable<unknown> = current.likedByCurrentUser
      ? this.likesApi.unlike(this.postId)
      : this.likesApi.like(this.postId);

    request.subscribe({
      // Re-read rather than adjusting the count locally. The server is the authority on
      // both the count and whether the like was accepted, and a guessed number that
      // disagrees with a refresh is worse than a brief wait.
      next: () => this.refreshPost(),
      error: (err: unknown) => {
        this.actionError.set(toMessage(err));
        this.busy.set(false);
      },
    });
  }

  protected addComment(): void {
    if (this.commentForm.invalid || this.busy()) {
      this.commentForm.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.actionError.set(null);

    this.commentsApi.create(this.postId, this.commentForm.getRawValue()).subscribe({
      next: () => {
        this.commentForm.reset({ body: '' });
        this.busy.set(false);
        this.refreshPost();

        // Land on the page that now holds the new comment. Derived from the count plus
        // one rather than from totalPages, which is stale here — and is 0 when this is
        // the first comment, which would ask for a page that does not exist.
        const total = (this.comments()?.totalCount ?? 0) + 1;
        this.loadComments(Math.ceil(total / COMMENT_PAGE_SIZE));
      },
      error: (err) => {
        this.actionError.set(toMessage(err));
        this.busy.set(false);
      },
    });
  }

  protected applyTag(): void {
    if (this.tagForm.invalid || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.actionError.set(null);

    this.tagsApi.apply(this.postId, this.tagForm.getRawValue().slug).subscribe({
      next: () => {
        this.tagForm.reset({ slug: '' });
        this.refreshPost();
      },
      error: (err) => {
        this.actionError.set(toMessage(err));
        this.busy.set(false);
      },
    });
  }

  protected removeTag(slug: string): void {
    this.busy.set(true);
    this.actionError.set(null);

    this.tagsApi.remove(this.postId, slug).subscribe({
      next: () => this.refreshPost(),
      error: (err) => {
        this.actionError.set(toMessage(err));
        this.busy.set(false);
      },
    });
  }

  private refreshPost(): void {
    this.postsApi.get(this.postId).subscribe({
      next: (post) => {
        this.post.set(post);
        this.busy.set(false);
      },
      error: (err) => {
        this.actionError.set(toMessage(err));
        this.busy.set(false);
      },
    });
  }
}
