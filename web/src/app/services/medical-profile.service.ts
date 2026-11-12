import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { MedicalProfile, RosterEntry } from '../models/medical-profile.model';

@Injectable({ providedIn: 'root' })
export class MedicalProfileService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/api`;

  getProfile(): Observable<MedicalProfile | null> {
    return this.http.get<MedicalProfile | null>(`${this.base}/profile/medical`);
  }

  saveProfile(dto: MedicalProfile): Observable<MedicalProfile> {
    return this.http.put<MedicalProfile>(`${this.base}/profile/medical`, dto);
  }

  getEmergencyRoster(activityId: number): Observable<RosterEntry[]> {
    return this.http.get<RosterEntry[]>(`${this.base}/activities/${activityId}/emergency-roster`);
  }
}
