import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'activities', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./auth/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./auth/register.component').then(m => m.RegisterComponent) },
  { path: 'activities', loadComponent: () => import('./activities/activities.component').then(m => m.ActivitiesComponent), canActivate: [authGuard] },
  { path: 'profile', loadComponent: () => import('./profile/profile.component').then(m => m.ProfileComponent), canActivate: [authGuard] },
];