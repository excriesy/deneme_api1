import React, { createContext, useContext, useState, useEffect } from 'react';
import authService from '../services/authService';

interface User {
    id: string;
    username: string;
    email: string;
    role: string;
}

interface LoginResponse {
    jwtToken: string;
    refreshToken: string;
    user: User;
}

interface AuthContextType {
    user: User | null;
    loading: boolean;
    login: (email: string, password: string) => Promise<void>;
    register: (username: string, email: string, password: string) => Promise<void>;
    logout: () => Promise<void>;
    isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [user, setUser] = useState<User | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const initAuth = async () => {
            try {
                if (authService.isAuthenticated()) {
                    const user = await authService.getCurrentUser();
                    setUser(user);
                }
            } catch (error) {
                console.error('Auth initialization error:', error);
            } finally {
                setLoading(false);
            }
        };

        initAuth();
    }, []);

    const login = async (email: string, password: string) => {
        try {
            const response = await authService.login(email, password);
            localStorage.setItem('token', response.jwtToken);
            localStorage.setItem('refreshToken', response.refreshToken);
            setUser(response.user);
        } catch (error) {
            console.error('Login error:', error);
            throw error;
        }
    };

    const register = async (username: string, email: string, password: string) => {
        try {
            await authService.register(username, email, password);
        } catch (error) {
            console.error('Register error:', error);
            throw error;
        }
    };

    const logout = async () => {
        try {
            await authService.logout();
            setUser(null);
        } catch (error) {
            console.error('Logout error:', error);
            throw error;
        }
    };

    const value = {
        user,
        loading,
        login,
        register,
        logout,
        isAuthenticated: !!user
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
}; 