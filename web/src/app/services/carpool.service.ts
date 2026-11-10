import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CreateCarpoolDto } from '../models/carpool.model';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CarpoolService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/api`;

  getCarpools(activityId: number) {
    return this.http.get<any[]>(`${this.base}/activities/${activityId}/carpools`);
  }

  createOffer(activityId: number, dto: CreateCarpoolDto) {
    return this.http.post<{ id: number }>(`${this.base}/activities/${activityId}/carpools`, dto);
  }

  join(offerId: number) {
    return this.http.post(`${this.base}/carpools/${offerId}/join`, {});
  }

  leave(offerId: number) {
    return this.http.delete(`${this.base}/carpools/${offerId}/join`);
  }

  deleteOffer(offerId: number) {
    return this.http.delete(`${this.base}/carpools/${offerId}`);
  }
}