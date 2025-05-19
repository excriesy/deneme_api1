import React from 'react';
import { Layout, Card } from 'antd';
import FileManager from '../components/FileManager';

const { Content } = Layout;

const Files: React.FC = () => {
    return (
        <Layout style={{ minHeight: '100vh' }}>
            <Content style={{ padding: '24px' }}>
                <Card title="Dosya Yöneticisi">
                    <FileManager />
                </Card>
            </Content>
        </Layout>
    );
};

export default Files; 