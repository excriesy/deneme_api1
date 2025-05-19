import axios from 'axios';
import { message } from 'antd';
import authService from './authService';

const api = axios.create({
    baseURL: process.env.REACT_APP_API_URL || 'http://localhost:5112/api',
    headers: {
        'Content-Type': 'application/json',
    },
});

// Request interceptor
api.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('token');
        if (token) {
            config.headers = config.headers || {};
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Response interceptor
api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        // Token geçersiz ve henüz yenileme denemesi yapılmamışsa
        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            try {
                const refreshToken = localStorage.getItem('refreshToken');
                if (!refreshToken) {
                    throw new Error('Refresh token bulunamadı');
                }

                const response = await authService.refreshToken(refreshToken);
                const { jwtToken } = response;

                localStorage.setItem('token', jwtToken);
                originalRequest.headers.Authorization = `Bearer ${jwtToken}`;

                return api(originalRequest);
            } catch (refreshError) {
                // Refresh token da geçersizse çıkış yap
                localStorage.removeItem('token');
                localStorage.removeItem('refreshToken');
                window.location.href = '/login';
                return Promise.reject(refreshError);
            }
        }

        // Diğer hata durumları
        if (error.response?.status === 403) {
            console.log('Yetkisiz erişim, çıkış yapılıyor...');
            window.location.href = '/login';
        }

        return Promise.reject(error);
    }
);

export default api; 