import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivityService } from '../services/activity.service';
import { CarpoolService } from '../services/carpool.service';
import { PotluckService } from '../services/potluck.service';
import { AuthService } from '../services/auth.service';
import { MedicalProfileService } from '../services/medical-profile.service';
import { IndexedDbService } from '../services/indexed-db.service';
import { Activity } from '../models/activity.model';
import { CarpoolOffer } from '../models/carpool.model';
import { PotluckItem, PotluckUnitLabels } from '../models/potluck.model';

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
  private potluckSvc = inject(PotluckService);
  private medSvc = inject(MedicalProfileService);
  private idb = inject(IndexedDbService);
  auth = inject(AuthService);

  // Activity state
  activities = signal<Activity[]>([]);
  loading = signal(false);
  error = signal('');
  showForm = signal(false);
  submitting = signal(false);
  newActivity = { title: '', description: '', scheduledDate: '', location: '', maxParticipants: 10 };

  // Carpool state
  carpools = signal<Record<number, CarpoolOffer[]>>({});
  showCarpoolSection = signal<Record<number, boolean>>({});
  showOfferForm = signal<Record<number, boolean>>({});
  offerForm: Record<number, any> = {};

  // Potluck state
  potluckItems = signal<Record<number, PotluckItem[]>>({});
  showPotluckSection = signal<Record<number, boolean>>({});
  showItemForm = signal<Record<number, boolean>>({});
  itemForm: Record<number, any> = {};
  claimQty: Record<number, number> = {};
  unitLabels = PotluckUnitLabels;

  // Roster state
  downloadingRoster = signal<Record<number, boolean>>({});

  get userRole(): string {
    return this.auth.currentUser()?.role ?? 'OE';
  }

  ngOnInit() { this.loadActivities(); }

  // ── Activities ──────────────────────────────────────────────

  loadActivities() {
    this.loading.set(true);
    this.activitySvc.getActivities().subscribe({
      next: list => { this.activities.set(list); this.loading.set(false); },
      error: () => { this.error.set('Failed to load activities.'); this.loading.set(false); }
    });
  }

  submit() {
    this.submitting.set(true);
    const payload = {
      ...this.newActivity,
      scheduledAt: this.newActivity.scheduledDate
    };
    this.activitySvc.createActivity(payload).subscribe({
      next: () => {
        this.submitting.set(false);
        this.showForm.set(false);
        this.newActivity = { title: '', description: '', scheduledDate: '', location: '', maxParticipants: 10 };
        this.loadActivities();
      },
      error: () => { this.submitting.set(false); }
    });
  }

  statusLabel(status: string | number): string {
    const map: Record<string, string> = {
      '0': 'Upcoming', 'upcoming': 'Upcoming',
      '1': 'Ongoing',  'ongoing': 'Ongoing',
      '2': 'Completed','completed': 'Completed',
      '3': 'Cancelled','cancelled': 'Cancelled'
    };
    return map[String(status).toLowerCase()] ?? String(status);
  }

  join(id: number) {
    this.activitySvc.attend(id).subscribe({ next: () => this.loadActivities() });
  }

  leave(id: number) {
    this.activitySvc.cancelAttendance(id).subscribe({ next: () => this.loadActivities() });
  }

  // ── Carpools ────────────────────────────────────────────────

  toggleCarpools(activityId: number) {
    const isOpen = this.showCarpoolSection()[activityId];
    this.showCarpoolSection.update(s => ({ ...s, [activityId]: !isOpen }));
    if (!isOpen) this.loadCarpools(activityId);
  }

  loadCarpools(activityId: number) {
    this.carpoolSvc.getCarpools(activityId).subscribe({
      next: offers => this.carpools.update(c => ({ ...c, [activityId]: offers }))
    });
  }

  toggleOfferForm(activityId: number) {
    const isOpen = this.showOfferForm()[activityId];
    if (!isOpen) {
      this.offerForm[activityId] = { totalSeats: 2, departureLocation: '', departureTime: '', notes: '' };
    }
    this.showOfferForm.update(s => ({ ...s, [activityId]: !isOpen }));
  }

  submitOffer(activityId: number) {
    const form = this.offerForm[activityId];
    this.carpoolSvc.createOffer(activityId, form).subscribe({
      next: () => {
        this.showOfferForm.update(s => ({ ...s, [activityId]: false }));
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

  // ── Potluck ─────────────────────────────────────────────────

  togglePotluck(activityId: number) {
    const isOpen = this.showPotluckSection()[activityId];
    this.showPotluckSection.update(s => ({ ...s, [activityId]: !isOpen }));
    if (!isOpen) this.loadPotluck(activityId);
  }

  loadPotluck(activityId: number) {
    this.potluckSvc.getItems(activityId).subscribe({
      next: items => this.potluckItems.update(p => ({ ...p, [activityId]: items }))
    });
  }

  toggleItemForm(activityId: number) {
    const isOpen = this.showItemForm()[activityId];
    if (!isOpen) {
      this.itemForm[activityId] = { name: '', unit: 0, quantityNeeded: 1, notes: '' };
    }
    this.showItemForm.update(s => ({ ...s, [activityId]: !isOpen }));
  }

  submitItem(activityId: number) {
    const form = this.itemForm[activityId];
    this.potluckSvc.addItem(activityId, { ...form, unit: Number(form.unit) }).subscribe({
      next: () => {
        this.showItemForm.update(s => ({ ...s, [activityId]: false }));
        this.loadPotluck(activityId);
      }
    });
  }

  claimItem(itemId: number, activityId: number) {
    const qty = this.claimQty[itemId] ?? 1;
    this.potluckSvc.claim(itemId, qty).subscribe({ next: () => this.loadPotluck(activityId) });
  }

  dropClaim(claimId: number, activityId: number) {
    this.potluckSvc.dropClaim(claimId).subscribe({ next: () => this.loadPotluck(activityId) });
  }

  deletePotluckItem(itemId: number, activityId: number) {
    this.potluckSvc.deleteItem(itemId).subscribe({ next: () => this.loadPotluck(activityId) });
  }

  // ── Roster (AD only) ────────────────────────────────────────

  async downloadRoster(activityId: number, activityTitle: string): Promise<void> {
    this.downloadingRoster.update(s => ({ ...s, [activityId]: true }));
    this.medSvc.getEmergencyRoster(activityId).subscribe({
      next: async (roster) => {
        await this.idb.saveRoster(activityId, activityTitle, roster);
        this.downloadingRoster.update(s => ({ ...s, [activityId]: false }));
        alert(`Roster for "${activityTitle}" saved for offline use (${roster.length} members).`);
      },
      error: () => {
        this.downloadingRoster.update(s => ({ ...s, [activityId]: false }));
      }
    });
  }
}
