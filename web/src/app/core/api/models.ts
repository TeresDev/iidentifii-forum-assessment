// Mirrors docs/api-contract.md. Field names match the wire exactly — renaming anything
// here silently decouples the client from the API, because an unrecognised query
// parameter is ignored server-side and still returns 200 with unfiltered data.

export type Role = 'User' | 'Moderator';

export type PostSort = 'date' | 'likes';

export type SortDirection = 'asc' | 'desc';

export interface UserSummary {
  id: string;
  username: string;
  role: Role;
}

export interface TagDto {
  slug: string;
  displayName: string;
}

export interface PostSummary {
  id: string;
  title: string;
  excerpt: string;
  authorId: string;
  authorUsername: string;
  createdAtUtc: string;
  likeCount: number;
  commentCount: number;
  tags: TagDto[];
  likedByCurrentUser: boolean;
}

export interface PostDetail {
  id: string;
  title: string;
  body: string;
  authorId: string;
  authorUsername: string;
  createdAtUtc: string;
  likeCount: number;
  commentCount: number;
  tags: TagDto[];
  likedByCurrentUser: boolean;
}

export interface CommentDto {
  id: string;
  postId: string;
  authorId: string;
  authorUsername: string;
  body: string;
  createdAtUtc: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: UserSummary;
}

/** `from` is inclusive, `to` is exclusive, so adjacent ranges tile without overlapping. */
export interface PostListQuery {
  page?: number;
  pageSize?: number;
  sort?: PostSort;
  dir?: SortDirection;
  author?: string;
  tag?: string;
  from?: string;
  to?: string;
}

export interface CreatePostRequest {
  title: string;
  body: string;
}

export interface CreateCommentRequest {
  body: string;
}

export interface LikeCountResponse {
  likeCount: number;
}

/** RFC 9457 ProblemDetails plus the stable `code` the API adds. */
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}
