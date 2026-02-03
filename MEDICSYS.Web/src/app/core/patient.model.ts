export interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  idNumber: string;
  dateOfBirth: string;
  gender: string;
  address: string;
  phone: string;
  email?: string;
  emergencyContact?: string;
  emergencyPhone?: string;
  allergies?: string;
  medications?: string;
  diseases?: string;
  bloodType?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
  source?: 'patient' | 'history';
  historyId?: string;
  hasClinicalHistory?: boolean;
  age?: number | null;
}

export interface PatientCreateRequest {
  firstName: string;
  lastName: string;
  idNumber: string;
  dateOfBirth: string;
  gender: string;
  phone: string;
  address?: string;
  email?: string;
  emergencyContact?: string;
  emergencyPhone?: string;
  allergies?: string;
  medications?: string;
  diseases?: string;
  bloodType?: string;
  notes?: string;
}
