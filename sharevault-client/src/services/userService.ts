import api from './api';

const API_URL = 'user';

export interface UserDto {
    id: string;
    email: string;
    username: string;
}

const userService = {
    /**
     * E-posta adresine göre kullanıcı bilgilerini getirir
     */
    getUserByEmail: async (email: string): Promise<UserDto> => {
        const response = await api.get(`${API_URL}/by-email/${encodeURIComponent(email)}`);
        return response.data as UserDto;
    }
};

export default userService;
