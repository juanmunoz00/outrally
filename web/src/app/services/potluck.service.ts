import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CreatePotluckItemDto, PotluckItem } from '../models/potluck.model';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PotluckService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/api`;

  getItems(activityId: number) {
    return this.http.get<PotluckItem[]>(`${this.base}/activities/${activityId}/potluck`);
  }

  addItem(activityId: number, dto: CreatePotluckItemDto) {
    return this.http.post<{ id: number }>(`${this.base}/activities/${activityId}/potluck`, dto);
  }

  updateItem(itemId: number, dto: CreatePotluckItemDto) {
    return this.http.put<{ id: number }>(`${this.base}/potluck/items/${itemId}`, dto);
  }

  deleteItem(itemId: number) {
    return this.http.delete(`${this.base}/potluck/items/${itemId}`);
  }

  claim(itemId: number, quantity: number) {
    return this.http.post(`${this.base}/potluck/items/${itemId}/claim`, { quantityClaimed: quantity });
  }

  dropClaim(claimId: number) {
    return this.http.delete(`${this.base}/potluck/claims/${claimId}`);
  }
}