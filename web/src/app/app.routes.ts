import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/posts/post-list/post-list').then((m) => m.PostList),
    title: 'Posts',
  },
  { path: '**', redirectTo: '' },
];
