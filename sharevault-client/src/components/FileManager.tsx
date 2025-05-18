import React, { useState, useEffect } from 'react';
import { Table, Button, Upload, message, Modal, Form, Input, Space } from 'antd';
import { UploadOutlined, DownloadOutlined, ShareAltOutlined, DeleteOutlined } from '@ant-design/icons';
import type { UploadFile } from 'antd/es/upload/interface';
import fileService, { FileDto } from '../services/fileService';

const FileManager: React.FC = () => {
    const [files, setFiles] = useState<FileDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [shareModalVisible, setShareModalVisible] = useState(false);
    const [selectedFile, setSelectedFile] = useState<FileDto | null>(null);
    const [shareForm] = Form.useForm();

    useEffect(() => {
        fetchFiles();
    }, []);

    const fetchFiles = async () => {
        try {
            setLoading(true);
            const data = await fileService.getFiles();
            setFiles(data);
        } catch (error: any) {
            message.error('Dosyalar yüklenirken bir hata oluştu: ' + error.message);
        } finally {
            setLoading(false);
        }
    };

    const handleUpload = async (file: File) => {
        try {
            await fileService.uploadFile(file);
            message.success('Dosya başarıyla yüklendi');
            fetchFiles();
        } catch (error: any) {
            message.error('Dosya yüklenirken bir hata oluştu: ' + error.message);
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
            message.error('Dosya indirilirken bir hata oluştu: ' + error.message);
        }
    };

    const handleDelete = async (fileId: string) => {
        try {
            await fileService.deleteFile(fileId);
            message.success('Dosya başarıyla silindi');
            fetchFiles();
        } catch (error: any) {
            message.error('Dosya silinirken bir hata oluştu: ' + error.message);
        }
    };

    const handleShare = async (values: { email: string }) => {
        if (!selectedFile) return;

        try {
            await fileService.shareFile(selectedFile.id, values.email);
            message.success('Dosya başarıyla paylaşıldı');
            setShareModalVisible(false);
            shareForm.resetFields();
        } catch (error: any) {
            message.error('Dosya paylaşılırken bir hata oluştu: ' + error.message);
        }
    };

    const columns = [
        {
            title: 'Dosya Adı',
            dataIndex: 'fileName',
            key: 'fileName',
        },
        {
            title: 'Boyut',
            dataIndex: 'fileSize',
            key: 'fileSize',
            render: (size: number) => `${(size / 1024 / 1024).toFixed(2)} MB`,
        },
        {
            title: 'Yüklenme Tarihi',
            dataIndex: 'uploadDate',
            key: 'uploadDate',
            render: (date: string) => new Date(date).toLocaleString(),
        },
        {
            title: 'İşlemler',
            key: 'actions',
            render: (_: any, record: FileDto) => (
                <Space>
                    <Button
                        icon={<DownloadOutlined />}
                        onClick={() => handleDownload(record.id, record.fileName)}
                    >
                        İndir
                    </Button>
                    <Button
                        icon={<ShareAltOutlined />}
                        onClick={() => {
                            setSelectedFile(record);
                            setShareModalVisible(true);
                        }}
                    >
                        Paylaş
                    </Button>
                    <Button
                        icon={<DeleteOutlined />}
                        danger
                        onClick={() => handleDelete(record.id)}
                    >
                        Sil
                    </Button>
                </Space>
            ),
        },
    ];

    return (
        <div>
            <div style={{ marginBottom: 16 }}>
                <Upload
                    customRequest={({ file }) => handleUpload(file as File)}
                    showUploadList={false}
                >
                    <Button icon={<UploadOutlined />}>Dosya Yükle</Button>
                </Upload>
            </div>

            <Table
                columns={columns}
                dataSource={files}
                rowKey="id"
                loading={loading}
            />

            <Modal
                title="Dosya Paylaş"
                open={shareModalVisible}
                onCancel={() => {
                    setShareModalVisible(false);
                    shareForm.resetFields();
                }}
                footer={null}
            >
                <Form form={shareForm} onFinish={handleShare}>
                    <Form.Item
                        name="email"
                        label="E-posta"
                        rules={[
                            { required: true, message: 'Lütfen e-posta adresini girin!' },
                            { type: 'email', message: 'Geçerli bir e-posta adresi girin!' }
                        ]}
                    >
                        <Input placeholder="E-posta adresi" />
                    </Form.Item>
                    <Form.Item>
                        <Button type="primary" htmlType="submit">
                            Paylaş
                        </Button>
                    </Form.Item>
                </Form>
            </Modal>
        </div>
    );
};

export default FileManager; 