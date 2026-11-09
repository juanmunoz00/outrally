export interface Activity {
  id: number;
  title: string;
  description: string;
  scheduledDate: string;
  location: string;
  maxParticipants: number;
  status: ActivityStatus;
  createdAt: string;
}

export enum ActivityStatus {
  Upcoming = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3
}
