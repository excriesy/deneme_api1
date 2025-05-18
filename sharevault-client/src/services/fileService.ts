import api from './api';

export interface FileDto {
    id: string;
    fileName: string;
    fileSize: number;
    uploadDate: string;
    downloadUrl: string;
}

const fileService = {
    getFiles: async (): Promise<FileDto[]> => {
        const response = await api.get<FileDto[]>('/api/File/list');
        return response.data;
    },

    uploadFile: async (file: File): Promise<FileDto> => {
        const formData = new FormData();
        formData.append('file', file);
        const response = await api.post<FileDto>('/api/File/upload', formData, {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
        });
        return response.data;
    },

    downloadFile: async (fileId: string): Promise<Blob> => {
        const response = await api.get(`/api/File/download/${fileId}`, {
            responseType: 'blob',
        });
        return response.data as Blob;
    },

    deleteFile: async (fileId: string): Promise<void> => {
        await api.delete(`/api/File/${fileId}`);
    },

    shareFile: async (fileId: string, email: string): Promise<void> => {
        await api.post(`/api/File/share`, { fileId, email });
    },

    getSharedFiles: async (): Promise<FileDto[]> => {
        const response = await api.get<FileDto[]>('/api/File/shared-files');
        return response.data;
    },

    getPublicFiles: async (): Promise<FileDto[]> => {
        const response = await api.get<FileDto[]>('/api/File/public-files');
        return response.data;
    },

    revokeAccess: async (fileId: string, sharedWithUserId: string): Promise<void> => {
        await api.post('/api/File/revoke-access', { fileId, sharedWithUserId });
    }
};

export default fileService; 