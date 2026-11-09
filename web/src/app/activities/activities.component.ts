import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivityService } from '../services/activity.service';
import { Activity, ActivityStatus } from '../models/activity.model';

@Component({
  selector: 'app-activities',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './activities.component.html',
  styleUrl: './activities.component.scss'
})
export class ActivitiesComponent implements OnInit {
  activities = signal<Activity[]>([]);
  loading = signal(true);
  error = signal('');
  showForm = signal(false);
  submitting = signal(false);

  form: FormGroup;

  constructor(private activityService: ActivityService, private fb: FormBuilder) {
    this.form = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      scheduledDate: ['', Validators.required],
      location: ['', Validators.required],
      maxParticipants: [10, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit(): void {
    this.loadActivities();
  }

  loadActivities(): void {
    this.loading.set(true);
    this.activityService.getAll().subscribe({
      next: (data) => {
        this.activities.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load activities.');
        this.loading.set(false);
      }
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.activityService.create(this.form.value).subscribe({
      next: () => {
        this.form.reset({ maxParticipants: 10 });
        this.showForm.set(false);
        this.submitting.set(false);
        this.loadActivities();
      },
      error: () => {
        this.error.set('Could not create activity.');
        this.submitting.set(false);
      }
    });
  }

  statusLabel(status: ActivityStatus): string {
    return ActivityStatus[status];
  }
}
