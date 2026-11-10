import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink],
  styleUrl: './navbar.component.scss',
  template: `
    <nav class="navbar">
      <span class="brand" routerLink="/activities">Outrally</span>
      <div class="nav-right">
        @if (auth.currentUser()) {
          <span class="user-info">
            {{ auth.currentUser()!.firstName }}
            <span class="role-badge">{{ auth.currentUser()!.role }}</span>
          </span>
          <button (click)="logout()">Logout</button>
        }
      </div>
    </nav>
  `
})
export class NavbarComponent {
  auth = inject(AuthService);
  router = inject(Router);
  logout() { this.auth.logout(); this.router.navigate(['/login']); }
}
