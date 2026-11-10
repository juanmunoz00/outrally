export interface CarpoolOffer {
  id: number;
  driverName: string;
  totalSeats: number;
  seatsAvailable: number;
  departureLocation: string;
  departureTime: string;
  notes?: string;
  isMyOffer: boolean;
  isMyRide: boolean;
}

export interface CreateCarpoolDto {
  totalSeats: number;
  departureLocation: string;
  departureTime: string;
  notes?: string;
}

export interface CarpoolOffer {
  id: number;
  driverName: string;
  totalSeats: number;
  seatsAvailable: number;
  departureLocation: string;
  departureTime: string;
  notes?: string;
  isMyOffer: boolean;
  isMyRide: boolean;
  passengers: string[];   // ← new
}