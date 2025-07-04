import api from './api';

export interface FileDto {
    id: string;
    name: string;
    contentType: string;
    size: number;
    uploadedAt: string;
    uploadedBy: string;
    userId: string;
    icon?: string;
    fileType?: string;
    isPreviewable?: boolean;
    folderId?: string | null;
    isFolder?: boolean;
    versionCount?: number;
}

export interface FileDetailsDto extends FileDto {
    path: string;
    isPublic: boolean;
    expiresAt?: string;
}

export interface SharedFileDto {
    id: string;
    fileId: string;
    sharedWithUserId: string;
    sharedAt: string;
    expiresAt?: string;
    canEdit: boolean;
}

export interface FileVersionDto {
    id: string;
    versionNumber: number;
    fileName: string;
    fileSize: number;
    uploadedAt: string;
    uploadedBy: string;
    changeNotes?: string;
}

interface UploadProgressEvent {
    loaded: number;
    total: number;
}

interface UploadConfig {
    headers: {
        'Content-Type': string;
    };
    onUploadProgress?: (progressEvent: UploadProgressEvent) => void;
    signal?: AbortSignal;
}

interface UserResponse {
    id: string;
    email: string;
    username: string;
}

interface TempUploadResponse {
    tempFileName: string;
    originalName: string;
}

const fileService = {
    async getFiles(folderId?: string | null): Promise<FileDto[]> {
        const response = await api.get<FileDto[]>('/file/list', {
            params: {
                parentFolderId: folderId,
                _t: Date.now()
            }
        });
        return response.data;
    },

    async createFolder(folderName: string, parentFolderId?: string | null): Promise<any> {
        const response = await api.post('/folder/create', {
            name: folderName,
            parentFolderId
        });
        return response.data;
    },

    async bulkDelete(fileIds: string[]): Promise<any> {
        const response = await api.post('/file/bulk-delete', {
            fileIds
        });
        return response.data;
    },

    async uploadTempFile(file: File, onProgress: (progressEvent: any) => void, signal: AbortSignal): Promise<{ tempFileName: string, originalName: string }> {
        const formData = new FormData();
        formData.append('file', file);

        const config: any = {
            headers: {
                'Content-Type': 'multipart/form-data'
            },
            onUploadProgress: onProgress,
            signal: signal
        };

        const response = await api.post('/file/upload-temp', formData, config);
        return response.data as { tempFileName: string, originalName: string };
    },

    async completeUpload(tempFileName: string, originalFileName: string, folderId?: string | null, changeNotes?: string): Promise<any> {
        const requestBody: any = {
            tempFileName,
            originalFileName,
        };

        if (folderId) {
            requestBody.folderId = folderId;
        }

        if (changeNotes) {
            requestBody.changeNotes = changeNotes;
        }

        const response = await api.post('/file/complete-upload', requestBody);
        return response.data;
    },

    async cancelUpload(tempFileName: string): Promise<any> {
        const response = await api.post('/file/cancel-upload', { tempFileName });
        return response.data;
    },

    async downloadFile(fileId: string): Promise<Blob> {
        const response = await api.get(`/file/download/${fileId}`, {
            responseType: 'blob'
        });
        return response.data as Blob;
    },

    async deleteFile(fileId: string): Promise<any> {
        const response = await api.delete(`/file/${fileId}`);
        return response.data;
    },

    async deleteFolder(folderId: string): Promise<any> {
        const response = await api.delete(`/folder/delete/${folderId}`);
        return response.data;
    },

    async shareFile(fileId: string, email: string): Promise<any> {
        const response = await api.post('/file/share-multiple', { 
            FileId: fileId, 
            UserEmails: [email] 
        });
        return response.data;
    },

    async getSharedFiles(): Promise<FileDto[]> {
        const response = await api.get<FileDto[]>('/file/shared-files');
        return response.data;
    },

    async getSharedUsers(fileId: string): Promise<{ fileId: string, fileName: string, sharedUsers: { userId: string, username: string, email: string, sharedAt: string }[] }> {
        const response = await api.get<{ fileId: string, fileName: string, sharedUsers: { userId: string, username: string, email: string, sharedAt: string }[] }>(`/file/shared-users/${fileId}`);
        return response.data;
    },

    async getPublicFiles(): Promise<FileDto[]> {
        const response = await api.get<FileDto[]>('/file/public-files');
        return response.data;
    },

    async revokeAccess(fileId: string, sharedWithUserId: string): Promise<void> {
        await api.post('/file/revoke-access', null, { params: { fileId, sharedWithUserId } });
    },

    async renameFolder(folderId: string, newName: string): Promise<void> {
        await api.put(`/folder/${folderId}?name=${encodeURIComponent(newName)}`);
    },

    async getFileVersions(fileId: string): Promise<FileVersionDto[]> {
        const response = await api.get<FileVersionDto[]>(`/file/${fileId}/versions`);
        return response.data;
    },

    async downloadFileVersion(fileId: string, versionNumber: number): Promise<Blob> {
        const response = await api.get(`/file/${fileId}/versions/${versionNumber}`, {
            responseType: 'blob'
        });
        return response.data as Blob;
    },

    async createFileVersion(fileId: string, changeNotes: string): Promise<void> {
        try {
            await api.post(`/file/${fileId}/versions`, { changeNotes });
        } catch (error) {
            console.error('Yeni versiyon oluşturulurken hata:', error);
            throw error;
        }
    },

    async startUpload(file: File): Promise<string> {
        try {
            const formData = new FormData();
            formData.append('file', file);
            
            const response = await api.post<{ tempFileName: string }>('/file/start-upload', formData, {
                headers: {
                    'Content-Type': 'multipart/form-data',
                },
            });
            
            return response.data.tempFileName;
        } catch (error) {
            console.error('Dosya yükleme başlatılırken hata:', error);
            throw error;
        }
    },

    async uploadFile(file: File, folderId?: string | null): Promise<void> {
        try {
            const formData = new FormData();
            formData.append('file', file);
            if (folderId) {
                formData.append('folderId', folderId);
            }
            
            await api.post('/file/upload', formData, {
                headers: {
                    'Content-Type': 'multipart/form-data',
                }
            });
        } catch (error) {
            console.error('Dosya yüklenirken hata:', error);
            throw error;
        }
    },
};

export default fileService; 