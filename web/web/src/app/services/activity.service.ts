import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Activity } from '../models/activity.model';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ActivityService {
  private url = `${environment.apiUrl}/api/activities`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Activity[]> {
    return this.http.get<Activity[]>(this.url);
  }
}
