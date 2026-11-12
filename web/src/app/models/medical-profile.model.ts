export interface MedicalProfile {
  bloodType: string;
  allergies: string;
  medications: string;
  medicalConditions: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  emergencyContactRelationship: string;
}

export interface RosterEntry {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  medicalProfile: MedicalProfile | null;
}

export const BLOOD_TYPES = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-', 'Unknown'];
