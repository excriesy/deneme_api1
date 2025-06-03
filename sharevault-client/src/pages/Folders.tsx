import React from 'react';
import { Layout, Card } from 'antd';
import FolderManager from '../components/FolderManager';

const { Content } = Layout;

const Folders: React.FC = () => {
    return (
        <Layout style={{ minHeight: '100vh' }}>
            <Content style={{ padding: '24px' }}>
                <Card title="Klasör Yöneticisi">
                    <FolderManager />
                </Card>
            </Content>
        </Layout>
    );
};

export default Folders;
