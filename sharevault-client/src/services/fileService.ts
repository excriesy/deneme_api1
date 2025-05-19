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
    async getFiles(): Promise<FileDto[]> {
        const response = await api.get<FileDto[]>('/file/list');
        return response.data;
    },

    async uploadTempFile(file: File, onProgress?: (progressEvent: UploadProgressEvent) => void, signal?: AbortSignal): Promise<TempUploadResponse> {
        const formData = new FormData();
        formData.append('file', file);

        const config: UploadConfig = {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
            onUploadProgress: (progressEvent: any) => {
                if (onProgress && progressEvent.total) {
                    onProgress({
                        loaded: progressEvent.loaded,
                        total: progressEvent.total
                    });
                }
            },
            signal
        };

        const response = await api.post<TempUploadResponse>('/file/upload-temp', formData, config);
        return response.data;
    },

    async completeUpload(tempFileName: string, originalFileName: string): Promise<string> {
        const response = await api.post<{ fileId: string }>('/file/complete-upload', {
            tempFileName,
            originalFileName
        });
        return response.data.fileId;
    },

    async cancelUpload(tempFileName: string): Promise<void> {
        await api.post('/file/cancel-upload', { tempFileName });
    },

    async downloadFile(fileId: string): Promise<Blob> {
        const response = await api.get(`/file/download/${fileId}`, {
            responseType: 'blob'
        });
        return response.data as Blob;
    },

    async deleteFile(fileId: string): Promise<void> {
        await api.delete(`/file/${fileId}`);
    },

    async shareFile(fileId: string, email: string): Promise<void> {
        const userResponse = await api.get<UserResponse>(`/user/by-email/${email}`);
        const userId = userResponse.data.id;
        
        await api.post('/file/share-multiple', {
            fileId,
            userIds: [userId]
        });
    },

    async getSharedFiles(): Promise<FileDto[]> {
        const response = await api.get<FileDto[]>('/file/shared-files');
        return response.data;
    },

    async getPublicFiles(): Promise<FileDto[]> {
        const response = await api.get<FileDto[]>('/file/public-files');
        return response.data;
    },

    async revokeAccess(fileId: string, sharedWithUserId: string): Promise<void> {
        await api.post('/file/revoke-access', { fileId, sharedWithUserId });
    }
};

export default fileService; 