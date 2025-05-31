import api from './api';
import { FolderDto, SharedFolderDto, PermissionType } from '../types/folderTypes';

// API endpoint (api.ts'deki baseURL'e eklenecek)
const API_URL = '/folder';

/**
 * Klasörler için servis fonksiyonları
 */
const folderService = {
    /**
     * Bir klasörü başka bir kullanıcıyla paylaşır
     */
    shareFolder: async (folderId: string, shareRequest: {
        sharedWithUserId: string;
        permission: PermissionType;
        expiresAt?: Date;
        shareNote?: string;
    }): Promise<boolean> => {
        // Parametrelerin kontrolü
        if (!folderId || !shareRequest.sharedWithUserId) {
            console.error('shareFolder: Geçersiz parametreler', { folderId, shareRequest });
            throw new Error('Klasör paylaşımı için gerekli parametreler eksik');
        }
        
        // Paylaşım isteğinin case-sensitive hale getirilmesi (backend PascalCase bekliyor olabilir)
        const formattedRequest = {
            SharedWithUserId: shareRequest.sharedWithUserId,
            Permission: shareRequest.permission,
            ExpiresAt: shareRequest.expiresAt,
            ShareNote: shareRequest.shareNote
        };
        
        console.log('Klasör paylaşım isteği gönderiliyor:', {
            endpoint: `/folder/${folderId}/share`,
            payload: formattedRequest
        });
        
        try {
            const response = await api.post(`${API_URL}/${folderId}/share`, formattedRequest);
            return response.data as boolean;
        } catch (error: any) {
            console.error('Klasör paylaşımı sırasında hata:', error.response?.data || error.message);
            throw error;
        }
    },

    /**
     * Kullanıcı ile paylaşılan klasörleri getirir
     */
    getSharedFolders: async (): Promise<FolderDto[]> => {
        const response = await api.get(`${API_URL}/shared-with-me`);
        return response.data as FolderDto[];
    },

    /**
     * Klasörün paylaşıldığı kullanıcıları getirir
     */
    getSharedUsers: async (folderId: string): Promise<any[]> => {
        const response = await api.get(`${API_URL}/${folderId}/shared-users`);
        return response.data as any[];
    },

    /**
     * Klasör erişimini iptal eder
     */
    revokeAccess: async (folderId: string, sharedWithUserId: string): Promise<boolean> => {
        // Parametrelerin kontrolü
        if (!folderId || !sharedWithUserId) {
            console.error('revokeAccess: Geçersiz parametreler', { folderId, sharedWithUserId });
            throw new Error('Erişim iptali için gerekli parametreler eksik');
        }
        
        // FolderId ve sharedWithUserId'nin formatını kontrol et - bunlar GUID olmalı
        const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        if (!guidRegex.test(folderId) || !guidRegex.test(sharedWithUserId)) {
            console.error('revokeAccess: Geçersiz GUID formatı', { folderId, sharedWithUserId });
            throw new Error('Erişim iptali için geçerli GUID ID\'ler gerekli');
        }
        
        // API URL'ini doğru şekilde oluştur
        const endpoint = `${API_URL}/${folderId}/revoke-access`;
        console.log('Klasör erişimi iptal ediliyor:', { folderId, sharedWithUserId, endpoint });
        
        try {
            // Eğer backend'de "Aktif paylaşım bulunamadı" hatası alıyorsak
            // paylaşım ID'si (folderId) ve kullanıcı ID'si (sharedWithUserId) kombinasyonu için
            // önce paylaşım olup olmadığını kontrol etmek iyi olabilir
            
            // API isteği için gerekli doğru formatta payload oluştur
            // Backend'in beklediği format: 'SharedWithUserId' (büyük harfle başlayan)
            const payload = { SharedWithUserId: sharedWithUserId };
            console.log('API isteği gönderiliyor:', { endpoint, payload, payloadJSON: JSON.stringify(payload) });
            
            // API isteği yap
            const response = await api.post(endpoint, payload);
            
            console.log('Erişim iptali başarılı yanıt:', response.data);
            return response.data as boolean;
        } catch (error: any) {
            const statusCode = error.response?.status;
            const responseData = error.response?.data;
            
            // Daha detaylı hata bilgisi
            console.error('Klasör erişimi iptal edilirken hata:', { 
                statusCode, 
                responseData, 
                message: error.message,
                requestPayload: { SharedWithUserId: sharedWithUserId },
                endpoint,
                errorObj: error
            });
            
            // 404 Not Found hatasını daha açıklayıcı bir mesajla yönet
            if (statusCode === 404) {
                throw new Error(`Erişim iptali yapılamadı: ${responseData || 'Aktif paylaşım bulunamadı'}. Klasör ve kullanıcı ID'lerini kontrol edin.`);
            }
            
            throw error;
        }
    },

    /**
     * Yeni klasör oluşturur
     */
    createFolder: async (name: string, parentFolderId?: string): Promise<FolderDto> => {
        const response = await api.post(API_URL, {
            name,
            parentFolderId
        });
        return response.data as FolderDto;
    },

    /**
     * Klasörü yeniden adlandırır
     */
    renameFolder: async (folderId: string, newName: string): Promise<FolderDto> => {
        const response = await api.put(`${API_URL}/${folderId}/rename`, {
            newName
        });
        return response.data as FolderDto;
    },

    /**
     * Klasörü siler
     */
    deleteFolder: async (folderId: string): Promise<boolean> => {
        const response = await api.delete(`${API_URL}/${folderId}`);
        return response.data as boolean;
    },

    /**
     * Klasörü başka bir klasöre taşır
     */
    moveFolder: async (folderId: string, targetFolderId?: string): Promise<FolderDto> => {
        const response = await api.put(`${API_URL}/${folderId}/move`, {
            targetFolderId
        });
        return response.data as FolderDto;
    },
};

export default folderService;
