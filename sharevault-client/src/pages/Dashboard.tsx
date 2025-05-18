import React, { useState, useEffect } from 'react';
import { Upload, Button, message, Card, List, Typography, Spin } from 'antd';
import { UploadOutlined, FileOutlined, DeleteOutlined } from '@ant-design/icons';
import type { UploadFile } from 'antd/es/upload/interface';
import { useAuth } from '../contexts/AuthContext';
import fileService, { FileDto } from '../services/fileService';

const { Title } = Typography;

const Dashboard: React.FC = () => {
    const [fileList, setFileList] = useState<UploadFile[]>([]);
    const [uploadedFiles, setUploadedFiles] = useState<FileDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [initialLoading, setInitialLoading] = useState(true);
    const { user } = useAuth();

    useEffect(() => {
        const fetchData = async () => {
            try {
                setInitialLoading(true);
                const files = await fileService.getFiles();
                setUploadedFiles(files);
            } catch (error: any) {
                console.error('Dosyalar yüklenirken hata:', error);
                message.error('Dosyalar yüklenirken bir hata oluştu: ' + error.message);
            } finally {
                setInitialLoading(false);
            }
        };

        fetchData();
    }, []);

    const handleUpload = async () => {
        try {
            setLoading(true);
            for (const file of fileList) {
                if (file.originFileObj) {
                    const formData = new FormData();
                    formData.append('file', file.originFileObj);
                    await fileService.uploadFile(file.originFileObj);
                }
            }
            message.success('Dosyalar başarıyla yüklendi');
            setFileList([]);
            const files = await fileService.getFiles();
            setUploadedFiles(files);
        } catch (error: any) {
            console.error('Dosya yükleme hatası:', error);
            message.error('Dosya yükleme başarısız: ' + error.message);
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async (fileId: string) => {
        try {
            await fileService.deleteFile(fileId);
            message.success('Dosya başarıyla silindi');
            const files = await fileService.getFiles();
            setUploadedFiles(files);
        } catch (error: any) {
            console.error('Dosya silme hatası:', error);
            message.error('Dosya silme başarısız: ' + error.message);
        }
    };

    const handleDownload = async (fileId: string, fileName: string) => {
        try {
            const response = await fileService.downloadFile(fileId);
            const url = window.URL.createObjectURL(new Blob([response]));
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', fileName);
            document.body.appendChild(link);
            link.click();
            link.remove();
        } catch (error: any) {
            console.error('Dosya indirme hatası:', error);
            message.error('Dosya indirme başarısız: ' + error.message);
        }
    };

    if (initialLoading) {
        return (
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
                <Spin size="large" />
            </div>
        );
    }

    return (
        <div style={{ padding: '24px' }}>
            <Title level={2}>Hoş Geldiniz, {user?.username}</Title>
            
            <Card title="Dosya Yükle" style={{ marginBottom: '24px' }}>
                <Upload
                    multiple
                    beforeUpload={() => false}
                    fileList={fileList}
                    onChange={({ fileList }) => setFileList(fileList)}
                >
                    <Button icon={<UploadOutlined />}>Dosya Seç</Button>
                </Upload>
                <Button
                    type="primary"
                    onClick={handleUpload}
                    disabled={fileList.length === 0}
                    loading={loading}
                    style={{ marginTop: '16px' }}
                >
                    Yükle
                </Button>
            </Card>

            <Card title="Dosyalarım">
                <List
                    dataSource={uploadedFiles}
                    renderItem={(file) => (
                        <List.Item
                            actions={[
                                <Button
                                    key="download"
                                    type="link"
                                    onClick={() => handleDownload(file.id, file.fileName)}
                                >
                                    İndir
                                </Button>,
                                <Button
                                    key="delete"
                                    type="link"
                                    danger
                                    onClick={() => handleDelete(file.id)}
                                >
                                    Sil
                                </Button>
                            ]}
                        >
                            <List.Item.Meta
                                avatar={<FileOutlined />}
                                title={file.fileName}
                                description={`Boyut: ${(file.fileSize / 1024 / 1024).toFixed(2)} MB`}
                            />
                        </List.Item>
                    )}
                />
            </Card>
        </div>
    );
};

export default Dashboard; 