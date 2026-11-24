import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="auth-container">
      <h2>Change Password</h2>
      <p>You must set a new password before continuing.</p>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <input formControlName="currentPassword" type="password" placeholder="Temporary password" required />
        <input formControlName="newPassword" type="password" placeholder="New password" required />
        <input formControlName="confirm" type="password" placeholder="Confirm new password" required />
        <div *ngIf="error()" class="error">{{ error() }}</div>
        <button type="submit" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Saving...' : 'Set New Password' }}
        </button>
      </form>
    </div>
  `
})
export class ChangePasswordComponent {
  form: FormGroup;
  loading = signal(false);
  error = signal('');

  constructor(private fb: FormBuilder, private http: HttpClient,
              private auth: AuthService, private router: Router) {
    this.form = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirm: ['', Validators.required]
    });
  }

  submit(): void {
    const { currentPassword, newPassword, confirm } = this.form.value;
    if (newPassword !== confirm) { this.error.set('Passwords do not match.'); return; }
    this.loading.set(true);
    this.error.set('');
    this.http.post<any>(`${environment.apiUrl}/api/auth/change-password`, {
      currentPassword, newPassword
    }).subscribe({
      next: (res) => {
        this.auth.setSession(res);
        this.router.navigate(['/activities']);
      },
      error: (e) => {
        this.error.set(e.error ?? 'Failed to change password.');
        this.loading.set(false);
      }
    });
  }
}