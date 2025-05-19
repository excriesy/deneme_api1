import React, { useState, useEffect } from 'react';
import { Table, Button, Upload, message, Modal, Form, Input, Space, Card, Typography, Progress, Image, Tooltip } from 'antd';
import { UploadOutlined, DownloadOutlined, ShareAltOutlined, DeleteOutlined, InboxOutlined, FileOutlined, IeOutlined } from '@ant-design/icons';
import type { UploadFile } from 'antd/es/upload/interface';
import fileService, { FileDto } from '../services/fileService';
import { useAuth } from '../contexts/AuthContext';

const { Title } = Typography;
const { Dragger } = Upload;

interface UploadProgressEvent {
    loaded: number;
    total: number;
}

const FileManager: React.FC = () => {
    const [files, setFiles] = useState<FileDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [uploading, setUploading] = useState(false);
    const [uploadProgress, setUploadProgress] = useState(0);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [shareModalVisible, setShareModalVisible] = useState(false);
    const [selectedFileForShare, setSelectedFileForShare] = useState<FileDto | null>(null);
    const [shareForm] = Form.useForm();
    const [previewVisible, setPreviewVisible] = useState(false);
    const [previewUrl, setPreviewUrl] = useState<string>('');
    const [previewImage, setPreviewImage] = useState<string>('');
    const [cancelToken, setCancelToken] = useState<AbortController | null>(null);
    const [tempFileName, setTempFileName] = useState<string | null>(null);
    const { user } = useAuth();

    useEffect(() => {
        loadFiles();
    }, []);

    const loadFiles = async () => {
        try {
            setLoading(true);
            const fileList = await fileService.getFiles();
            setFiles(fileList);
        } catch (error: any) {
            message.error('Dosyalar yüklenirken bir hata oluştu: ' + error.message);
            setFiles([]);
        } finally {
            setLoading(false);
        }
    };

    const handleFileSelect = async (file: File) => {
        setSelectedFile(file);
        setUploadProgress(0);
        setUploading(true);

        if (file.type.startsWith('image/')) {
            const reader = new FileReader();
            reader.onload = (e) => {
                setPreviewImage(e.target?.result as string);
            };
            reader.readAsDataURL(file);
        } else {
            setPreviewImage('');
        }

        try {
            const controller = new AbortController();
            setCancelToken(controller);

            const response = await fileService.uploadTempFile(file, (progressEvent: UploadProgressEvent) => {
                const percentCompleted = Math.round((progressEvent.loaded * 100) / progressEvent.total);
                setUploadProgress(percentCompleted);
            }, controller.signal);

            setTempFileName(response.tempFileName);
            setUploading(false);
        } catch (error: any) {
            if (error.name === 'AbortError') {
                message.info('Dosya yükleme iptal edildi');
            } else {
                message.error('Dosya yüklenirken bir hata oluştu: ' + error.message);
            }
            setSelectedFile(null);
            setPreviewImage('');
            setUploadProgress(0);
            setTempFileName(null);
        } finally {
            setCancelToken(null);
        }
        
        return false;
    };

    const handleCompleteUpload = async () => {
        if (!selectedFile || !tempFileName) return;

        try {
            setLoading(true);
            await fileService.completeUpload(tempFileName, selectedFile.name);
            message.success('Dosya başarıyla yüklendi');
            setSelectedFile(null);
            setPreviewImage('');
            setUploadProgress(0);
            setTempFileName(null);
            await loadFiles();
        } catch (error: any) {
            message.error('Dosya yüklenirken bir hata oluştu: ' + error.message);
        } finally {
            setLoading(false);
        }
    };

    const handleCancelUpload = async () => {
        if (cancelToken) {
            cancelToken.abort();
        }
        if (tempFileName) {
            try {
                await fileService.cancelUpload(tempFileName);
            } catch (error) {
                console.error('Geçici dosya silinirken hata oluştu:', error);
            }
        }
        setUploading(false);
        setUploadProgress(0);
        setCancelToken(null);
        setSelectedFile(null);
        setPreviewImage('');
        setTempFileName(null);
    };

    const handleDownload = async (fileId: string, fileName: string) => {
        try {
            const blob = await fileService.downloadFile(fileId);
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = fileName;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
            message.success('Dosya indirme başladı');
        } catch (error: any) {
            message.error('Dosya indirilirken bir hata oluştu: ' + (error.message || ''));
            if (error.message && (error.message.includes('404') || error.message.includes('Not Found'))) {
                setFiles(files.filter(f => f.id !== fileId));
            }
        }
    };

    const handleDelete = async (fileId: string) => {
        Modal.confirm({
            title: 'Dosyayı silmek istediğinize emin misiniz?',
            content: 'Bu işlem geri alınamaz.',
            okText: 'Evet',
            okType: 'danger',
            cancelText: 'Hayır',
            onOk: async () => {
                try {
                    setLoading(true);
                    await fileService.deleteFile(fileId);
                    message.success('Dosya başarıyla silindi');
                    setFiles(files.filter(f => f.id !== fileId));
                } catch (error: any) {
                    message.error('Dosya silinirken bir hata oluştu: ' + (error.message || ''));
                    if (error.message && (error.message.includes('404') || error.message.includes('Not Found'))) {
                        setFiles(files.filter(f => f.id !== fileId));
                    }
                } finally {
                    setLoading(false);
                }
            }
        });
    };

    const handleShare = async (values: { email: string }) => {
        if (!selectedFileForShare) return;

        try {
            setLoading(true);
            await fileService.shareFile(selectedFileForShare.id, values.email);
            message.success('Dosya başarıyla paylaşıldı');
            setShareModalVisible(false);
            shareForm.resetFields();
        } catch (error: any) {
            message.error('Dosya paylaşılırken bir hata oluştu: ' + error.message);
        } finally {
            setLoading(false);
        }
    };

    const formatFileSize = (bytes: number) => {
        if (bytes === 0) return '0 B';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    };

    const handlePreview = async (fileId: string, fileName: string) => {
        // Sadece resim dosyaları için önizleme
        const fileToPreview = files.find(f => f.id === fileId);
        if (!fileToPreview || !fileToPreview.contentType.startsWith('image/')) {
            message.info('Bu dosya türü için önizleme desteklenmiyor.');
            return;
        }

        try {
            const blob = await fileService.downloadFile(fileId);
            const url = window.URL.createObjectURL(blob);
            setPreviewUrl(url);
            setPreviewVisible(true);
        } catch (error: any) {
            message.error('Dosya önizlemesi alınırken bir hata oluştu: ' + (error.message || ''));
        }
    };

    const handleClosePreview = () => {
        setPreviewVisible(false);
        setPreviewUrl('');
    };

    const columns = [
        {
            title: 'Dosya Adı',
            dataIndex: 'name',
            key: 'name',
            render: (text: string, record: FileDto) => (
                <div style={{ display: 'flex', alignItems: 'center' }}>
                    <span style={{ fontSize: '18px', marginRight: 8, flexShrink: 0 }}>{record.icon || '📄'}</span>
                    <span style={{ wordBreak: 'break-word', flexGrow: 1 }}>{text}</span>
                </div>
            ),
        },
        {
            title: 'Boyut',
            dataIndex: 'size',
            key: 'size',
            render: (size: number) => formatFileSize(size),
        },
        {
            title: 'Yükleyen',
            dataIndex: 'uploadedBy',
            key: 'uploadedBy',
        },
        {
            title: 'Yüklenme Tarihi',
            dataIndex: 'uploadedAt',
            key: 'uploadedAt',
            render: (date: string) => new Date(date).toLocaleString('tr-TR'),
        },
        {
            title: 'İşlemler',
            key: 'actions',
            render: (_: any, record: FileDto) => (
                <Space>
                    {record.contentType.startsWith('image/') && (
                        <Tooltip title="Önizle">
                            <Button
                                icon={<IeOutlined />}
                                onClick={() => handlePreview(record.id, record.name)}
                                type="text"
                            />
                        </Tooltip>
                    )}
                    <Tooltip title="İndir">
                        <Button
                            icon={<DownloadOutlined />}
                            onClick={() => handleDownload(record.id, record.name)}
                            type="text"
                        />
                    </Tooltip>
                    <Tooltip title="Paylaş">
                        <Button
                            icon={<ShareAltOutlined />}
                            onClick={() => {
                                setSelectedFileForShare(record);
                                setShareModalVisible(true);
                            }}
                            type="text"
                        />
                    </Tooltip>
                    {record.userId === user?.id && (
                        <Tooltip title="Sil">
                            <Button
                                icon={<DeleteOutlined />}
                                onClick={() => handleDelete(record.id)}
                                type="text"
                                danger
                            />
                        </Tooltip>
                    )}
                </Space>
            ),
        },
    ];

    return (
        <div>
            <Card title="Dosya Yükle" style={{ marginBottom: 16 }}>
                <Dragger
                    accept="*/*"
                    beforeUpload={handleFileSelect}
                    showUploadList={false}
                    disabled={uploading}
                >
                    <p className="ant-upload-drag-icon">
                        <InboxOutlined />
                    </p>
                    <p className="ant-upload-text">Dosyayı buraya sürükleyin veya seçmek için tıklayın</p>
                </Dragger>

                {selectedFile && (
                    <div style={{ marginTop: 16 }}>
                        <Space direction="vertical" style={{ width: '100%' }}>
                            <div>
                                <p>Yüklenen dosya: {selectedFile.name}</p>
                                <p>Boyut: {formatFileSize(selectedFile.size)}</p>
                                {previewImage && (
                                    <div style={{ marginTop: 8 }}>
                                        <Image
                                            src={previewImage}
                                            alt="Dosya önizleme"
                                            style={{ maxWidth: '200px', maxHeight: '200px', objectFit: 'contain' }}
                                            preview={false}
                                        />
                                    </div>
                                )}
                            </div>
                            
                            <Progress 
                                percent={uploadProgress} 
                                status={uploading ? "active" : "success"}
                                format={(percent) => `${percent}%`}
                            />
                            
                            {!uploading ? (
                                <Space>
                                    <Button
                                        type="primary"
                                        onClick={handleCompleteUpload}
                                        loading={loading}
                                    >
                                        Yükle
                                    </Button>
                                    <Button
                                        onClick={handleCancelUpload}
                                    >
                                        İptal
                                    </Button>
                                </Space>
                            ) : (
                                <Button
                                    onClick={handleCancelUpload}
                                    danger
                                >
                                    İptal Et
                                </Button>
                            )}
                        </Space>
                    </div>
                )}
            </Card>

            <Card title="Dosyalarım">
                <Table
                    columns={columns}
                    dataSource={files}
                    rowKey="id"
                    loading={loading}
                    locale={{
                        emptyText: 'Henüz dosya yüklenmemiş'
                    }}
                />
            </Card>

            <Modal
                title={`${selectedFileForShare?.name} Dosyasını Paylaş`}
                open={shareModalVisible}
                onCancel={() => {
                    setShareModalVisible(false);
                    setSelectedFileForShare(null);
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
                        <Input placeholder="Paylaşılacak kullanıcının e-posta adresi" />
                    </Form.Item>
                    <Form.Item>
                        <Space>
                            <Button type="primary" htmlType="submit" loading={loading}>
                                Paylaş
                            </Button>
                            <Button onClick={() => {
                                setShareModalVisible(false);
                                setSelectedFileForShare(null);
                                shareForm.resetFields();
                            }}>
                                İptal
                            </Button>
                        </Space>
                    </Form.Item>
                </Form>
            </Modal>

            {/* Resim Önizleme Modalı */}
            <Modal
                open={previewVisible}
                footer={null}
                onCancel={handleClosePreview}
                width={600}
            >
                <img src={previewUrl} alt="Dosya Önizleme" style={{ width: '100%' }} />
            </Modal>
        </div>
    );
};

export default FileManager; 