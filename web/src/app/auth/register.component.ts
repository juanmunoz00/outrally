import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <h2>Create Account</h2>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <input formControlName="firstName" placeholder="First Name" required />
        <input formControlName="lastName" placeholder="Last Name" required />
        <input formControlName="email" type="email" placeholder="Email" required />
        <input formControlName="password" type="password" placeholder="Password (min 8 chars)" required />
        <div *ngIf="error()" class="error">{{ error() }}</div>
        <div *ngIf="success()" class="success">{{ success() }}</div>
        <button type="submit" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Creating...' : 'Create Account' }}
        </button>
      </form>
      <p>Already have an account? <a routerLink="/login">Sign in</a></p>
    </div>
  `
})
export class RegisterComponent {
  form: FormGroup;
  loading = signal(false);
  error = signal('');
  success = signal('');

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]]
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set('');
    const { email, password, firstName, lastName } = this.form.value;
    this.auth.register(email, password, firstName, lastName).subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => {
        this.error.set('Registration failed. Try a different email.');
        this.loading.set(false);
      }
    });
  }
}
