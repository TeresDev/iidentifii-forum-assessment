import { authGuard } from './core/auth/auth-guard';
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/posts/post-list/post-list').then((m) => m.PostList),
    title: 'Posts',
  },
  {
    path: 'posts/new',
    loadComponent: () =>
      import('./features/posts/post-create/post-create').then((m) => m.PostCreate),
    canActivate: [authGuard],
    title: 'New post',
  },
  {
    path: 'posts/:id',
    loadComponent: () =>
      import('./features/posts/post-detail/post-detail').then((m) => m.PostDetail),
    title: 'Post',
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
    title: 'Sign in',
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
    title: 'Register',
  },
  { path: '**', redirectTo: '' },
];
