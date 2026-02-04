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

export type InvoiceStatus = 'Pending' | 'Authorized' | 'Rejected';

export interface InvoiceCustomer {
  identificationType: string;
  identification: string;
  name: string;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
}

export interface InvoiceItem {
  id?: string;
  description: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  subtotal?: number;
  taxRate?: number;
  tax?: number;
  total?: number;
}

export interface Invoice {
  id: string;
  number: string;
  sequential: number;
  issuedAt: string;
  customer: InvoiceCustomer;
  subtotal: number;
  discountTotal: number;
  tax: number;
  total: number;
  cardFeePercent?: number | null;
  cardFeeAmount?: number | null;
  totalToCharge: number;
  paymentMethod: string;
  cardType?: string | null;
  cardInstallments?: number | null;
  paymentReference?: string | null;
  observations?: string | null;
  status: InvoiceStatus;
  sriAccessKey?: string | null;
  sriAuthorizationNumber?: string | null;
  sriAuthorizedAt?: string | null;
  sriMessages?: string | null;
  items: InvoiceItem[];
}

export interface AccountingCategory {
  id: string;
  name: string;
  group: string;
  type: 'Income' | 'Expense';
  monthlyBudget: number;
  isActive: boolean;
}

export interface AccountingEntry {
  id: string;
  date: string;
  type: 'Income' | 'Expense';
  categoryId: string;
  categoryName: string;
  categoryGroup: string;
  description: string;
  amount: number;
  paymentMethod?: string | null;
  reference?: string | null;
  source: string;
  invoiceId?: string | null;
}

export interface AccountingSummary {
  from: string;
  to: string;
  totalIncome: number;
  totalExpense: number;
  net: number;
  profit?: number;
  profitMargin?: number;
  incomePercentChange?: number;
  expensePercentChange?: number;
  groups: { group: string; type: string; total: number }[];
}

export interface InventoryItem {
  id: string | number;
  name: string;
  description?: string;
  sku?: string;
  quantity: number;
  minimumQuantity: number;
  unitPrice: number;
  supplier?: string;
  expirationDate?: string;
  isLowStock?: boolean;
  isExpiringSoon?: boolean;
  createdAt?: string;
  updatedAt?: string;
  // Aliases para compatibilidad backend
  category?: string;
  minQuantity?: number;
  purchasePrice?: number;
  salePrice?: number;
}

export interface PurchaseItem {
  inventoryItemId: string | number;
  inventoryItemName: string;
  quantity: number;
  unitPrice: number;
  expirationDate?: string;
}

export interface PurchaseOrder {
  id: number;
  supplier: string;
  invoiceNumber?: string;
  purchaseDate: string;
  notes?: string;
  items: PurchaseItem[];
  total: number;
  status: 'Pending' | 'Received';
  createdAt?: string;
  updatedAt?: string;
}

export interface Expense {
  id: string;
  odontologoId: string;
  description: string;
  amount: number;
  expenseDate: string;
  category: string;
  paymentMethod: string;
  invoiceNumber?: string;
  supplier?: string;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ExpenseSummary {
  totalExpenses: number;
  monthExpenses: number;
  weekExpenses: number;
  expensesByCategory: Record<string, number>;
  recentExpenses: Expense[];
}
