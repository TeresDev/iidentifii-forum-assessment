import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommentsApi } from '../../../core/api/comments-api';
import { CommentDto, PagedResult, PostDetail as PostDetailModel } from '../../../core/api/models';
import { PostsApi } from '../../../core/api/posts-api';
import { toMessage } from '../../../core/api/problem-details';

const COMMENT_PAGE_SIZE = 10;

@Component({
  selector: 'app-post-detail',
  imports: [RouterLink, DatePipe],
  templateUrl: './post-detail.html',
  styleUrl: './post-detail.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly postsApi = inject(PostsApi);
  private readonly commentsApi = inject(CommentsApi);

  // Component-scoped rather than an injectable store: this state belongs to one screen
  // and is not read anywhere else. Same three-signal shape either way.
  protected readonly post = signal<PostDetailModel | null>(null);
  protected readonly comments = signal<PagedResult<CommentDto> | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected postId = '';

  ngOnInit(): void {
    this.postId = this.route.snapshot.paramMap.get('id') ?? '';
    this.load();
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
    this.commentsApi.list(this.postId, page, COMMENT_PAGE_SIZE).subscribe({
      next: (result) => this.comments.set(result),
      error: (err) => this.error.set(toMessage(err)),
    });
  }

  protected previousComments(): void {
    this.loadComments((this.comments()?.page ?? 1) - 1);
  }

  protected nextComments(): void {
    this.loadComments((this.comments()?.page ?? 1) + 1);
  }
}
