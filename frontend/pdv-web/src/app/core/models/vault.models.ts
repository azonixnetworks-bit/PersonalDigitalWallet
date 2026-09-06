export interface FolderDto {
  id: number;
  name: string;
  createdAt: string;
}

export interface CreateFolderRequest {
  name: string;
}

export interface UpdateFolderRequest {
  name: string;
}

export interface DocumentDto {
  id: number;
  fileName: string;
  contentType: string;
  fileSize: number;
  fileHash: string;
  folderId: number | null;
  createdAt: string;
}

export interface UpdateDocumentRequest {
  fileName: string;
}

export interface DocumentShareDto {
  shareId: number;
  documentId: number;
  recipientUserId: number;
  recipientName: string;
  recipientEmail: string;
  status: string;
  sharedAt: string;
  expiresAt: string;
  acceptedAt: string | null;
  revokedAt: string | null;
}

export interface ShareDocumentRequest {
  recipientEmail: string;
}

export type SecureUploadStage =
  | 'idle'
  | 'preparing'
  | 'uploading'
  | 'encrypting'
  | 'complete'
  | 'error';

export interface SecureUploadProgress {
  stage: SecureUploadStage;
  percent: number;
  title: string;
  detail: string;
}

export type DocumentUploadEvent =
  | { type: 'progress'; uploadPercent: number; securePercent: number }
  | { type: 'processing' }
  | { type: 'complete'; document: DocumentDto };

export interface CredentialDto {
  id: number;
  title: string;
  username: string;
  password: string;
  website: string | null;
  notes: string | null;
}

export interface CreateCredentialRequest {
  title: string;
  username: string;
  password: string;
  website: string | null;
  notes: string | null;
}

export type UpdateCredentialRequest = CreateCredentialRequest;
