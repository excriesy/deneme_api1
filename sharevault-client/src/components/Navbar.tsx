import React from 'react';
import { Layout, Menu, Button } from 'antd';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { HomeOutlined, LogoutOutlined, LoginOutlined, UserAddOutlined } from '@ant-design/icons';

const { Header } = Layout;

const Navbar: React.FC = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const { isAuthenticated, logout, user } = useAuth();

    const handleLogout = async () => {
        try {
            await logout();
            navigate('/login');
        } catch (error) {
            console.error('Çıkış yapılırken hata oluştu:', error);
        }
    };

    const menuItems = isAuthenticated
        ? [
            {
                key: '/',
                icon: <HomeOutlined />,
                label: 'Ana Sayfa',
                onClick: () => navigate('/')
            },
            {
                key: 'logout',
                icon: <LogoutOutlined />,
                label: 'Çıkış Yap',
                onClick: handleLogout
            }
        ]
        : [
            {
                key: '/login',
                icon: <LoginOutlined />,
                label: 'Giriş Yap',
                onClick: () => navigate('/login')
            },
            {
                key: '/register',
                icon: <UserAddOutlined />,
                label: 'Kayıt Ol',
                onClick: () => navigate('/register')
            }
        ];

    return (
        <Header style={{ 
            display: 'flex', 
            justifyContent: 'space-between', 
            alignItems: 'center',
            background: '#fff',
            padding: '0 24px',
            boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
        }}>
            <div style={{ 
                fontSize: '20px', 
                fontWeight: 'bold',
                color: '#1890ff'
            }}>
                ShareVault
            </div>
            <Menu
                mode="horizontal"
                selectedKeys={[location.pathname]}
                items={menuItems}
                style={{ 
                    border: 'none',
                    background: 'transparent'
                }}
            />
        </Header>
    );
};

export default Navbar; 