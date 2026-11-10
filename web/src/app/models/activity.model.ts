import { CarpoolOffer } from './carpool.model';

export interface Activity {
  id: number;
  title: string;
  description: string;
  scheduledDate: string;
  location: string;
  maxParticipants: number;
  status: string;
  createdAt: string;
  attendeeCount: number;
  isAttending: boolean;
  carpools: CarpoolOffer[];
}

export enum ActivityStatus {
  Upcoming = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}
