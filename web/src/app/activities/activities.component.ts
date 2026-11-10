import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivityService } from '../services/activity.service';
import { CarpoolService } from '../services/carpool.service';
import { Activity } from '../models/activity.model';
import { CarpoolOffer } from '../models/carpool.model';

@Component({
  selector: 'app-activities',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './activities.component.html',
  styleUrl: './activities.component.scss'
})
export class ActivitiesComponent implements OnInit {
  private activitySvc = inject(ActivityService);
  private carpoolSvc = inject(CarpoolService);

  activities = signal<Activity[]>([]);
  loading = signal(false);
  error = signal('');
  showForm = signal(false);
  submitting = signal(false);

  // carpool state keyed by activityId
  carpools = signal<Record<number, CarpoolOffer[]>>({});
  showCarpoolSection = signal<Record<number, boolean>>({});
  showOfferForm = signal<Record<number, boolean>>({});
  offerForm: Record<number, any> = {};

  newActivity = { title: '', description: '', scheduledDate: '', location: '', maxParticipants: 10 };

  ngOnInit() { this.loadActivities(); }

  loadActivities() {
    this.loading.set(true);
    this.activitySvc.getActivities().subscribe({
      next: list => { this.activities.set(list); this.loading.set(false); },
      error: () => { this.error.set('Failed to load activities.'); this.loading.set(false); }
    });
  }

  submit() {
    this.submitting.set(true);
    this.activitySvc.createActivity(this.newActivity).subscribe({
      next: () => {
        this.showForm.set(false);
        this.newActivity = { title: '', description: '', scheduledDate: '', location: '', maxParticipants: 10 };
        this.loadActivities();
        this.submitting.set(false);
      },
      error: () => { this.submitting.set(false); }
    });
  }

  join(id: number) {
    this.activitySvc.attend(id).subscribe({ next: () => this.loadActivities() });
  }

  leave(id: number) {
    this.activitySvc.cancelAttendance(id).subscribe({ next: () => this.loadActivities() });
  }

  statusLabel(status: string) {
    const map: Record<string, string> = { '0': 'Scheduled', '1': 'Cancelled', '2': 'Completed' };
    return map[status] ?? status;
  }

  // Carpool methods
  toggleCarpools(activityId: number) {
    const current = this.showCarpoolSection();
    const isOpen = current[activityId];
    this.showCarpoolSection.set({ ...current, [activityId]: !isOpen });
    if (!isOpen) this.loadCarpools(activityId);
  }

  loadCarpools(activityId: number) {
    this.carpoolSvc.getCarpools(activityId).subscribe({
      next: list => this.carpools.set({ ...this.carpools(), [activityId]: list })
    });
  }

  toggleOfferForm(activityId: number) {
    const current = this.showOfferForm();
    this.showOfferForm.set({ ...current, [activityId]: !current[activityId] });
    if (!this.offerForm[activityId]) {
      this.offerForm[activityId] = { totalSeats: 3, departureLocation: '', departureTime: '', notes: '' };
    }
  }

  submitOffer(activityId: number) {
    const form = this.offerForm[activityId];
    this.carpoolSvc.createOffer(activityId, form).subscribe({
      next: () => {
        this.showOfferForm.set({ ...this.showOfferForm(), [activityId]: false });
        this.loadCarpools(activityId);
      }
    });
  }

  joinCarpool(offerId: number, activityId: number) {
    this.carpoolSvc.join(offerId).subscribe({ next: () => this.loadCarpools(activityId) });
  }

  leaveCarpool(offerId: number, activityId: number) {
    this.carpoolSvc.leave(offerId).subscribe({ next: () => this.loadCarpools(activityId) });
  }

  deleteOffer(offerId: number, activityId: number) {
    this.carpoolSvc.deleteOffer(offerId).subscribe({ next: () => this.loadCarpools(activityId) });
  }

isInAnyRide(offer: CarpoolOffer, offers: CarpoolOffer[]): boolean {
  return (offers ?? []).some(o => o.isMyRide && o.id !== offer.id);
}

}