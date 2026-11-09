import { Component } from '@angular/core';
import { ActivitiesComponent } from './activities/activities.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ActivitiesComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {}
