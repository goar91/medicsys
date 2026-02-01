export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  role: string;
  universityId?: string | null;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: UserProfile;
}

export type ClinicalHistoryStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected';

export interface ClinicalHistory {
  id: string;
  studentId: string;
  studentName: string;
  status: ClinicalHistoryStatus;
  data: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
  submittedAt?: string | null;
  reviewedAt?: string | null;
  reviewNotes?: string | null;
}

export interface ClinicalHistoryReviewRequest {
  approved: boolean;
  notes?: string | null;
}
