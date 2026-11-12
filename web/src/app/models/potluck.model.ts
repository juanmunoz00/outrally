export enum PotluckUnit { Package = 0, Bottle = 1, Can = 2, Bag = 3, Box = 4, Other = 5 }

export const PotluckUnitLabels: Record<number, string> = {
  0: 'Package', 1: 'Bottle', 2: 'Can', 3: 'Bag', 4: 'Box', 5: 'Other'
};

export interface PotluckClaim {
  id: number;
  claimantName: string;
  quantityClaimed: number;
}

export interface PotluckItem {
  id: number;
  name: string;
  unit: number;
  quantityNeeded: number;
  quantityClaimed: number;
  quantityRemaining: number;
  isFullyClaimed: boolean;
  isMyItem: boolean;
  notes?: string;
  myClaim?: { id: number; quantityClaimed: number };
  claims: PotluckClaim[];
}

export interface CreatePotluckItemDto {
  name: string;
  unit: number;
  quantityNeeded: number;
  notes?: string;
}