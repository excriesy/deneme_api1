import React from 'react';
import { Layout, Menu, Button, Space } from 'antd';
import { HomeOutlined, FileOutlined, UserOutlined, LogoutOutlined } from '@ant-design/icons';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const { Header } = Layout;

const Navbar: React.FC = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const { user, logout } = useAuth();

    const handleLogout = () => {
        logout();
        navigate('/login');
    };

    const menuItems = [
        {
            key: '/dashboard',
            icon: <HomeOutlined />,
            label: 'Ana Sayfa',
        },
        {
            key: '/files',
            icon: <FileOutlined />,
            label: 'Dosyalarım',
        },
    ];

    return (
        <Header style={{ 
            display: 'flex', 
            alignItems: 'center', 
            justifyContent: 'space-between',
            background: '#fff',
            padding: '0 24px',
            boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
        }}>
            <div style={{ display: 'flex', alignItems: 'center' }}>
                <div style={{ 
                    fontSize: '20px', 
                    fontWeight: 'bold', 
                    marginRight: '48px',
                    color: '#1890ff'
                }}>
                    ShareVault
                </div>
                <Menu
                    mode="horizontal"
                    selectedKeys={[location.pathname]}
                    items={menuItems}
                    onClick={({ key }) => navigate(key)}
                    style={{ border: 'none' }}
                />
            </div>
            <Space>
                <span style={{ marginRight: 16 }}>
                    <UserOutlined /> {user?.email}
                </span>
                <Button 
                    icon={<LogoutOutlined />} 
                    onClick={handleLogout}
                >
                    Çıkış Yap
                </Button>
            </Space>
        </Header>
    );
};

export default Navbar; 