import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MedicalProfileService } from '../services/medical-profile.service';
import { IndexedDbService, CachedRoster } from '../services/indexed-db.service';
import { MedicalProfile, RosterEntry, BLOOD_TYPES } from '../models/medical-profile.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  private svc = inject(MedicalProfileService);
  private idb = inject(IndexedDbService);

  bloodTypes = BLOOD_TYPES;

  loading = signal(false);
  saving = signal(false);
  saved = signal(false);
  error = signal('');

  form = signal<MedicalProfile>({
    bloodType: 'Unknown',
    allergies: '',
    medications: '',
    medicalConditions: '',
    emergencyContactName: '',
    emergencyContactPhone: '',
    emergencyContactRelationship: ''
  });

  // Offline rosters (AD only)
  cachedRosters = signal<Omit<CachedRoster, 'roster'>[]>([]);
  viewingRoster = signal<CachedRoster | null>(null);

  ngOnInit(): void {
    this.loadProfile();
    this.loadCachedRosters();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.svc.getProfile().subscribe({
      next: (p) => {
        if (p) this.form.set({ ...p });
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load profile.');
        this.loading.set(false);
      }
    });
  }

  updateField(field: keyof MedicalProfile, value: string): void {
    this.form.update(f => ({ ...f, [field]: value }));
  }

  save(): void {
    this.saving.set(true);
    this.saved.set(false);
    this.error.set('');
    this.svc.saveProfile(this.form()).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), 3000);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to save profile.');
      }
    });
  }

  // ── Offline Rosters (cached by AD from activities page) ──────────────────

  async loadCachedRosters(): Promise<void> {
    try {
      const list = await this.idb.listRosters();
      this.cachedRosters.set(list);
    } catch { /* IndexedDB not available */ }
  }

  async viewRoster(activityId: number): Promise<void> {
    const cached = await this.idb.getRoster(activityId);
    this.viewingRoster.set(cached);
  }

  closeRoster(): void {
    this.viewingRoster.set(null);
  }

  async deleteRoster(activityId: number): Promise<void> {
    await this.idb.deleteRoster(activityId);
    this.cachedRosters.update(r => r.filter(x => x.activityId !== activityId));
    if (this.viewingRoster()?.activityId === activityId) this.viewingRoster.set(null);
  }
}
