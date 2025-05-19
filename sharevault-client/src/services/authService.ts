import api from './api';

interface LoginResponse {
    jwtToken: string;
    refreshToken: string;
    user: {
        id: string;
        username: string;
        email: string;
        role: string;
    };
}

interface User {
    id: string;
    username: string;
    email: string;
    role: string;
}

const authService = {
    login: async (email: string, password: string): Promise<LoginResponse> => {
        try {
            const response = await api.post<LoginResponse>('/Auth/login', { email, password });
            if (response.data.jwtToken) {
                localStorage.setItem('token', response.data.jwtToken);
                localStorage.setItem('refreshToken', response.data.refreshToken);
            }
            return response.data;
        } catch (error: any) {
            console.error('Login error:', error.response?.data || error.message);
            if (error.response) {
                throw new Error(error.response.data.message || 'Giriş başarısız oldu');
            }
            throw new Error('Sunucuya bağlanılamadı');
        }
    },

    register: async (username: string, email: string, password: string): Promise<void> => {
        try {
            await api.post('/Auth/register', { username, email, password });
        } catch (error: any) {
            console.error('Register error:', error.response?.data || error.message);
            if (error.response) {
                throw new Error(error.response.data.message || 'Kayıt başarısız oldu');
            }
            throw new Error('Sunucuya bağlanılamadı');
        }
    },

    logout: async (): Promise<void> => {
        try {
            const token = localStorage.getItem('token');
            if (token) {
                await api.post('/Auth/logout');
            }
        } catch (error: any) {
            console.error('Logout error:', error.response?.data || error.message);
        } finally {
            localStorage.removeItem('token');
            localStorage.removeItem('refreshToken');
        }
    },

    async refreshToken(refreshToken: string): Promise<LoginResponse> {
        try {
            const response = await api.post<LoginResponse>('/Auth/refresh', { refreshToken });
            if (response.data.jwtToken) {
                localStorage.setItem('token', response.data.jwtToken);
                localStorage.setItem('refreshToken', response.data.refreshToken);
            }
            return response.data;
        } catch (error: any) {
            console.error('Refresh token error:', error.response?.data || error.message);
            if (error.response) {
                throw new Error(error.response.data.message || 'Token yenileme başarısız oldu');
            }
            throw new Error('Sunucuya bağlanılamadı');
        }
    },

    getCurrentUser: async (): Promise<User> => {
        try {
            const token = localStorage.getItem('token');
            if (!token) {
                throw new Error('Token bulunamadı');
            }

            const response = await api.get<User>('/Auth/me');
            return response.data;
        } catch (error: any) {
            console.error('Get current user error:', error.response?.data || error.message);
            if (error.response) {
                throw new Error(error.response.data.message || 'Kullanıcı bilgileri alınamadı');
            }
            throw new Error('Sunucuya bağlanılamadı');
        }
    },

    isAuthenticated(): boolean {
        const token = localStorage.getItem('token');
        return !!token;
    }
};

export default authService; 