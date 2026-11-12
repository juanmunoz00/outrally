import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivityService } from '../services/activity.service';
import { CarpoolService } from '../services/carpool.service';
import { PotluckService } from '../services/potluck.service';
import { AuthService } from '../services/auth.service';
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

  // ── Carpools ─────────────────────────────────────────────────

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

  // ── Potluck ──────────────────────────────────────────────────

  togglePotluck(activityId: number) {
    const current = this.showPotluckSection();
    const isOpen = current[activityId];
    this.showPotluckSection.set({ ...current, [activityId]: !isOpen });
    if (!isOpen) this.loadPotluck(activityId);
  }

  loadPotluck(activityId: number) {
    this.potluckSvc.getItems(activityId).subscribe({
      next: items => this.potluckItems.set({ ...this.potluckItems(), [activityId]: items })
    });
  }

  toggleItemForm(activityId: number) {
    const current = this.showItemForm();
    this.showItemForm.set({ ...current, [activityId]: !current[activityId] });
    if (!this.itemForm[activityId]) {
      this.itemForm[activityId] = { name: '', unit: 0, quantityNeeded: 1, notes: '' };
    }
  }

  submitItem(activityId: number) {
    this.potluckSvc.addItem(activityId, this.itemForm[activityId]).subscribe({
      next: () => {
        this.showItemForm.set({ ...this.showItemForm(), [activityId]: false });
        this.loadPotluck(activityId);
      }
    });
  }

  deletePotluckItem(itemId: number, activityId: number) {
    this.potluckSvc.deleteItem(itemId).subscribe({ next: () => this.loadPotluck(activityId) });
  }

  claimItem(itemId: number, activityId: number) {
    const qty = this.claimQty[itemId] ?? 1;
    this.potluckSvc.claim(itemId, qty).subscribe({ next: () => this.loadPotluck(activityId) });
  }

  dropClaim(claimId: number, activityId: number) {
    this.potluckSvc.dropClaim(claimId).subscribe({ next: () => this.loadPotluck(activityId) });
  }
}
