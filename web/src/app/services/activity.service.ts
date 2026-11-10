import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Activity } from '../models/activity.model';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ActivityService {
  private url = `${environment.apiUrl}/api/activities`;

  constructor(private http: HttpClient) {}

  getActivities(): Observable<Activity[]> {
    return this.http.get<Activity[]>(this.url);
  }

  createActivity(activity: Partial<Activity>): Observable<Activity> {
    return this.http.post<Activity>(this.url, activity);
  }

  attend(id: number): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/attend`, {});
  }

  cancelAttendance(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}/attend`);
  }
}