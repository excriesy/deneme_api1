import React from 'react';
import { Typography, Card } from 'antd';
import { useAuth } from '../contexts/AuthContext';
import FileManager from '../components/FileManager';

const { Title } = Typography;

const Dashboard: React.FC = () => {
    const { user } = useAuth();

    return (
        <div style={{ padding: '24px' }}>
            <Title level={2}>Hoş Geldiniz, {user?.username}</Title>
            
            <Card>
                <FileManager />
            </Card>
        </div>
    );
};

export default Dashboard; 