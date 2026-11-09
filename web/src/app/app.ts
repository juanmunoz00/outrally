import { Component } from '@angular/core';
import { ActivitiesComponent } from './activities/activities.component';

@Component({
  imports: [ActivitiesComponent],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {}
