import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime } from 'rxjs';
import { LookupsApi } from '../../../core/api/lookups-api';
import { PostSort, SortDirection, TagDto, UserSummary } from '../../../core/api/models';
import { PostsStore } from '../posts-store';

@Component({
  selector: 'app-post-list',
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './post-list.html',
  styleUrl: './post-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostList implements OnInit {
  protected readonly store = inject(PostsStore);
  private readonly lookups = inject(LookupsApi);
  private readonly fb = inject(FormBuilder);

  protected readonly tags = signal<TagDto[]>([]);
  protected readonly authors = signal<UserSummary[]>([]);

  protected readonly filters = this.fb.nonNullable.group({
    author: '',
    tag: '',
    from: '',
    to: '',
    sort: 'date' as PostSort,
    dir: 'desc' as SortDirection,
  });

  ngOnInit(): void {
    this.lookups.tags().subscribe((tags) => this.tags.set(tags));
    this.lookups.users().subscribe((users) => this.authors.set(users));

    // Debounced because the date inputs fire on every keystroke while a date is typed.
    this.filters.valueChanges.pipe(debounceTime(250)).subscribe(() => this.apply());

    this.store.refresh();
  }

  protected apply(): void {
    const { author, tag, from, to, sort, dir } = this.filters.getRawValue();

    this.store.applyFilters({
      author,
      tag,
      sort,
      dir,
      from: from ? new Date(from).toISOString() : '',
      to: to ? new Date(to).toISOString() : '',
    });
  }

  protected clear(): void {
    this.filters.reset({ author: '', tag: '', from: '', to: '', sort: 'date', dir: 'desc' });
  }

  protected previousPage(): void {
    this.store.goToPage(this.store.page() - 1);
  }

  protected nextPage(): void {
    this.store.goToPage(this.store.page() + 1);
  }
}
