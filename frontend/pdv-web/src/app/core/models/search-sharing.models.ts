export type VaultSearchResultType = 'Folder' | 'Document' | 'Credential';

export interface VaultSearchResult {
  type: VaultSearchResultType;
  id: number;
  title: string;
}

export type VaultSearchFilter = 'all' | 'folder' | 'document' | 'credential';

export interface SharedDocumentDto {
  shareId: number;
  documentId: number;
  fileName: string;
  contentType: string;
  fileSize: number;
  sharedBy: string;
  sharedByEmail: string;
  status: string;
  sharedAt: string;
  acceptedAt: string | null;
}

export interface AcceptShareRequest {
  token: string;
}

export interface MessageResponse {
  message: string;
}
