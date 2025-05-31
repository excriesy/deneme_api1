/**
 * Klasör DTO sınıfı
 */
export interface FolderDto {
    id: string;
    name: string;
    parentFolderId: string | null;
    createdAt: string;
    createdById: string;
    createdByUsername: string;
    isFolder: boolean;
    size?: number;
    uploadDate?: string;
    uploaderId?: string;
    uploaderUsername?: string;
}

/**
 * İzin türleri
 */
/**
 * İzin türleri - Backend enum değerleriyle uyumlu (numeric) olmalıdır
 */
export enum PermissionType {
    Read = 0,
    Write = 1,
    Delete = 2,
    Share = 3,
    FullControl = 4
}

/**
 * Paylaşılan klasör DTO sınıfı
 */
export interface SharedFolderDto {
    id: string;
    folderId: string;
    folderName: string;
    sharedByUserId: string;
    sharedByUsername: string;
    sharedWithUserId: string;
    sharedWithUsername: string;
    permission: PermissionType;
    isActive: boolean;
    sharedAt: string;
    expiresAt: string | null;
    shareNote: string | null;
}

/**
 * Klasör içeriği DTO sınıfı
 */
export interface FolderContentsDto {
    folder: FolderDto;
    subFolders: FolderDto[];
    files: any[]; // FileDto[] türüne dönüştürebilirsiniz
    breadcrumbs: FolderBreadcrumbDto[];
}

/**
 * Klasör breadcrumb DTO sınıfı
 */
export interface FolderBreadcrumbDto {
    id: string;
    name: string;
}
