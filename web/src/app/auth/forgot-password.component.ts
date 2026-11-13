import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <h2>Forgot Password</h2>
      @if (sent()) {
        <p class="success">If that email exists, a reset link has been sent. Check your inbox.</p>
        <p><a routerLink="/login">Back to Sign In</a></p>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()">
          <input formControlName="email" type="email" placeholder="Email" required />
          <div *ngIf="error()" class="error">{{ error() }}</div>
          <button type="submit" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Sending...' : 'Send Reset Link' }}
          </button>
        </form>
        <p><a routerLink="/login">Back to Sign In</a></p>
      }
    </div>
  `
})
export class ForgotPasswordComponent {
  form: FormGroup;
  loading = signal(false);
  error = signal('');
  sent = signal(false);

  constructor(private fb: FormBuilder, private http: HttpClient) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set('');
    this.http.post(`${environment.apiUrl}/api/auth/forgot-password`, {
      email: this.form.value.email,
      appBaseUrl: window.location.origin
    }).subscribe({
      next: () => { this.sent.set(true); this.loading.set(false); },
      error: () => { this.error.set('Something went wrong. Try again.'); this.loading.set(false); }
    });
  }
}