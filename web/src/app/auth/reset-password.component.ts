import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <h2>Reset Password</h2>
      @if (done()) {
        <p class="success">Password reset successfully!</p>
        <p><a routerLink="/login">Sign In</a></p>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()">
          <input formControlName="newPassword" type="password" placeholder="New password" required />
          <input formControlName="confirm" type="password" placeholder="Confirm password" required />
          <div *ngIf="error()" class="error">{{ error() }}</div>
          <button type="submit" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Resetting...' : 'Reset Password' }}
          </button>
        </form>
      }
    </div>
  `
})
export class ResetPasswordComponent implements OnInit {
  form: FormGroup;
  loading = signal(false);
  error = signal('');
  done = signal(false);

  private token = '';
  private email = '';

  constructor(private fb: FormBuilder, private http: HttpClient,
              private route: ActivatedRoute, private router: Router) {
    this.form = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirm: ['', Validators.required]
    });
  }

  ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    if (!this.token || !this.email) this.error.set('Invalid reset link.');
  }

  submit(): void {
    const { newPassword, confirm } = this.form.value;
    if (newPassword !== confirm) { this.error.set('Passwords do not match.'); return; }
    this.loading.set(true);
    this.error.set('');
    this.http.post(`${environment.apiUrl}/api/auth/reset-password`, {
      email: this.email,
      token: this.token,
      newPassword
    }).subscribe({
      next: () => { this.done.set(true); this.loading.set(false); },
      error: (e) => {
        this.error.set(e.error ?? 'Token is invalid or expired.');
        this.loading.set(false);
      }
    });
  }
}