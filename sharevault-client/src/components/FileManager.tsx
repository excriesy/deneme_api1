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
    const [sharedFiles, setSharedFiles] = useState<FileDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [loadingSharedFiles, setLoadingSharedFiles] = useState(false);
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
    const [currentFolderId, setCurrentFolderId] = useState<string | null>(null);
    const [createFolderModalVisible, setCreateFolderModalVisible] = useState(false);
    const [createFolderForm] = Form.useForm();
    const [searchText, setSearchText] = useState('');
    const [selectedRowKeys, setSelectedRowKeys] = useState<string[]>([]);
    const [currentView, setCurrentView] = useState<'myFiles' | 'sharedFiles'>('myFiles');

    // Yeni state'ler: Paylaşım detayları için
    const [sharedUsersModalVisible, setSharedUsersModalVisible] = useState(false);
    const [selectedFileForSharedUsers, setSelectedFileForSharedUsers] = useState<FileDto | null>(null);
    const [fileSharedUsers, setFileSharedUsers] = useState<any[]>([]); // Paylaşılan kullanıcı listesi
    const [loadingSharedUsers, setLoadingSharedUsers] = useState(false); // Paylaşılan kullanıcılar listesi yükleniyor mu?

    useEffect(() => {
        loadFiles();
        loadSharedFiles();
    }, [user]);

    useEffect(() => {
        if (currentView === 'myFiles') {
            loadFiles();
        }
    }, [currentFolderId, currentView]);

    const loadFiles = async () => {
        try {
            console.log('loadFiles started. Current folderId:', currentFolderId);
            setLoading(true);
            const fileList = await fileService.getFiles(currentFolderId);
            console.log('loadFiles: File list from backend:', fileList);
            setFiles(fileList);
            console.log('loadFiles: File list state set.');
        } catch (error: any) {
            console.error('Error loading files:', error);
            message.error('Dosyalar yüklenirken bir hata oluştu: ' + error.message);
            setFiles([]);
        } finally {
            setLoading(false);
            console.log('loadFiles finished.');
        }
    };

    const loadSharedFiles = async () => {
        try {
            setLoadingSharedFiles(true);
            const sharedFileList = await fileService.getSharedFiles();
            setSharedFiles(sharedFileList);
        } catch (error: any) {
            message.error('Paylaşılan dosyalar yüklenirken bir hata oluştu: ' + error.message);
            setSharedFiles([]);
        } finally {
            setLoadingSharedFiles(false);
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
            await fileService.completeUpload(tempFileName, selectedFile.name, currentFolderId);
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
                    await loadFiles();
                } catch (error: any) {
                    message.error('Dosya silinirken bir hata oluştu: ' + (error.message || ''));
                    await loadFiles();
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

    const parentFolderItem = files.find(item => item.id === currentFolderId && item.contentType === 'folder');
    const parentFolderId = parentFolderItem?.folderId;

    const columns = [
        {
            title: 'Dosya Adı',
            dataIndex: 'name',
            key: 'name',
            render: (text: string, record: FileDto) => (
                <div 
                    style={{ display: 'flex', alignItems: 'center', cursor: record.contentType === 'folder' ? 'pointer' : 'default' }}
                    onClick={() => {
                        if (record.contentType === 'folder') {
                            setCurrentFolderId(record.id);
                        }
                    }}
                >
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
                    {record.contentType.startsWith('image/') && (record.isPreviewable ?? true) && (
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
                    <Tooltip title="Paylaş">
                        <Button
                            icon={<ShareAltOutlined />}
                            onClick={() => {
                                if (record.userId === user?.id) {
                                    setSelectedFileForShare(record);
                                    setShareModalVisible(true);
                                } else {
                                    message.info('Bu dosyayı paylaşma yetkiniz yok.');
                                }
                            }}
                            type="text"
                        />
                    </Tooltip>
                    <Tooltip title="Paylaşım Detayları">
                         <Button
                             icon={<FileOutlined />}
                             onClick={() => {
                                if (record.userId === user?.id) {
                                    // Kendi dosyanızsa paylaşım detaylarını göster
                                    handleViewSharedUsers(record.id, record.name);
                                } else {
                                    // Başkasının sizinle paylaştığı dosyaysa yetki uyarısı ver
                                    message.info('Bu dosyanın paylaşım detaylarını görme yetkiniz yok.');
                                }
                            }}
                             type="text"
                         />
                    </Tooltip>
                </Space>
            ),
        },
    ];

    const sharedFilesColumns = [
        {
            title: 'Dosya Adı',
            dataIndex: 'name',
            key: 'name',
            render: (text: string, record: FileDto) => (
                <div style={{ display: 'flex', alignItems: 'center' }}>
                    <span style={{ fontSize: '18px', marginRight: 8 }}>{record.icon || '📄'}</span>
                    <span>{text}</span>
                </div>
            ),
        },
        {
            title: 'Paylaşan Kişi',
            dataIndex: 'uploadedBy',
            key: 'uploadedBy',
        },
        {
            title: 'Boyut',
            dataIndex: 'size',
            key: 'size',
            render: (size: number) => formatFileSize(size),
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
                    {record.contentType.startsWith('image/') && (record.isPreviewable ?? true) && (
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
                     <Tooltip title="Paylaşım Detayları">
                         <Button
                             icon={<FileOutlined />}
                             onClick={() => {
                                if (record.userId === user?.id) {
                                    // Kendi dosyanızsa paylaşım detaylarını göster
                                    handleViewSharedUsers(record.id, record.name);
                                } else {
                                    // Başkasının sizinle paylaştığı dosyaysa yetki uyarısı ver
                                    message.info('Bu dosyanın paylaşım detaylarını görme yetkiniz yok.');
                                }
                            }}
                             type="text"
                         />
                    </Tooltip>
                </Space>
            ),
        },
    ];

    const handleCreateFolder = async (values: { folderName: string }) => {
        try {
            setLoading(true);
            await fileService.createFolder(values.folderName, currentFolderId);
            message.success('Klasör başarıyla oluşturuldu');
            setCreateFolderModalVisible(false);
            createFolderForm.resetFields();
            await loadFiles();
        } catch (error: any) {
            message.error('Klasör oluşturulurken bir hata oluştu: ' + error.message);
        } finally {
            setLoading(false);
        }
    };

    const handleBulkDelete = async () => {
        if (selectedRowKeys.length === 0) {
            message.warning('Lütfen silinecek dosyaları seçin');
            return;
        }

        Modal.confirm({
            title: 'Seçili dosyaları silmek istediğinize emin misiniz?',
            content: 'Bu işlem geri alınamaz.',
            okText: 'Evet',
            okType: 'danger',
            cancelText: 'Hayır',
            onOk: async () => {
                try {
                    setLoading(true);
                    await fileService.bulkDelete(selectedRowKeys);
                    message.success('Seçili dosyalar başarıyla silindi');
                    setSelectedRowKeys([]);
                    await loadFiles();
                } catch (error: any) {
                    message.error('Dosyalar silinirken bir hata oluştu: ' + error.message);
                    await loadFiles();
                } finally {
                    setLoading(false);
                }
            }
        });
    };

    const rowSelection = {
        selectedRowKeys,
        onChange: (newSelectedRowKeys: React.Key[], selectedRows: FileDto[]) => {
            setSelectedRowKeys(newSelectedRowKeys.map(key => key.toString()));
        },
    };

    const filteredFiles = files.filter(file => 
        file.name.toLowerCase().includes(searchText.toLowerCase())
    );

    // Yeni fonksiyon: Paylaşılan kullanıcıları görmek için
    const handleViewSharedUsers = async (fileId: string, fileName: string) => {
        setSelectedFileForSharedUsers({ id: fileId, name: fileName } as FileDto); // Sadece gerekli bilgileri set et
        setSharedUsersModalVisible(true);
        setLoadingSharedUsers(true);
        try {
            // Backend'den paylaşılan kullanıcıları çekecek fileService metodu çağrılacak
            const sharedUsersList = await fileService.getSharedUsers(fileId);
            console.log('API yanıtı tamamı:', sharedUsersList); // Yeni log
            console.log('API yanıtındaki sharedUsers:', sharedUsersList.sharedUsers); // sharedUsers küçük harf yapıldı
            setFileSharedUsers(sharedUsersList.sharedUsers); // sharedUsers küçük harf yapıldı
        } catch (error: any) {
            message.error('Paylaşım detayları yüklenirken bir hata oluştu: ' + error.message);
            setFileSharedUsers([]);
        } finally {
            setLoadingSharedUsers(false);
        }
    };

    // fileSharedUsers state'i değiştiğinde konsola yazdırmak için useEffect ekledim
    useEffect(() => {
        console.log('fileSharedUsers state güncellendi:', fileSharedUsers);
    }, [fileSharedUsers]);

    // Yeni fonksiyon: Paylaşım erişimini kaldırmak için
    const handleRevokeAccess = async (fileId: string, sharedWithUserId: string) => {
         Modal.confirm({
            title: 'Paylaşım Erişimini Kaldır',
            content: 'Bu kullanıcının dosyaya erişimini kaldırmak istediğinize emin misiniz?',
            okText: 'Evet',
            okType: 'danger',
            cancelText: 'Hayır',
            onOk: async () => {
                 try {
                    console.log('Erişim kaldırma isteği gönderiliyor. File ID:', fileId, 'Shared With User ID:', sharedWithUserId); // Log ekledim
                    // Backend'e erişimi kaldırma isteği gönderecek fileService metodu çağrılacak
                    await fileService.revokeAccess(fileId, sharedWithUserId);
                    message.success('Erişim başarıyla kaldırıldı.');
                    // Modalı yeniden yükleyerek listeyi güncelle
                    if(selectedFileForSharedUsers) {
                        handleViewSharedUsers(selectedFileForSharedUsers.id, selectedFileForSharedUsers.name);
                    }
                 } catch (error: any) {
                     message.error('Erişim kaldırılırken bir hata oluştu: ' + error.message);
                 }
            }
         });
    };

    return (
        <div style={{ padding: '20px' }}>
            <Title level={2}>Dosya Yöneticisi</Title>

            <Space style={{ marginBottom: '20px' }}>
                <Button
                    type={currentView === 'myFiles' ? 'primary' : 'default'}
                    onClick={() => setCurrentView('myFiles')}
                >
                    Dosyalarım
                </Button>
                <Button
                    type={currentView === 'sharedFiles' ? 'primary' : 'default'}
                    onClick={() => setCurrentView('sharedFiles')}
                >
                    Benimle Paylaşılanlar
                </Button>
                {currentView === 'myFiles' && currentFolderId !== null && (
                    <Button
                        onClick={() => setCurrentFolderId(parentFolderId || null)}
                    >
                        Geri
                    </Button>
                )}
                {currentView === 'myFiles' && currentFolderId === null && (
                    <>
                        <Button
                            type="primary"
                            onClick={() => setCreateFolderModalVisible(true)}
                        >
                            Yeni Klasör
                        </Button>
                        <Input.Search
                            placeholder="Dosya ara..."
                            allowClear
                            onSearch={value => setSearchText(value)}
                            style={{ width: 200 }}
                        />
                    </>
                )}
                {currentView === 'myFiles' && selectedRowKeys.length > 0 && (
                    <Button
                        danger
                        onClick={handleBulkDelete}
                    >
                        Seçili Dosyaları Sil ({selectedRowKeys.length})
                    </Button>
                )}
            </Space>

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

            {currentView === 'myFiles' && (
                <Card title="Dosyalarım" style={{ marginBottom: 16 }}>
                    <Table
                        columns={columns}
                        dataSource={filteredFiles}
                        rowKey="id"
                        loading={loading}
                        rowSelection={currentFolderId === null ? rowSelection : undefined}
                        locale={{
                            emptyText: 'Henüz dosya yüklenmemiş'
                        }}
                    />
                </Card>
            )}

            {currentView === 'sharedFiles' && (
                <Card title="Benimle Paylaşılan Dosyalar" style={{ marginBottom: 16 }}>
                    <Table
                        columns={sharedFilesColumns}
                        dataSource={sharedFiles}
                        rowKey="id"
                        loading={loadingSharedFiles}
                        locale={{
                            emptyText: 'Benimle paylaşılan dosya bulunamadı'
                        }}
                    />
                </Card>
            )}

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

            <Modal
                open={previewVisible}
                footer={null}
                onCancel={handleClosePreview}
                width={600}
            >
                <img src={previewUrl} alt="Dosya Önizleme" style={{ width: '100%' }} />
            </Modal>

            <Modal
                title="Yeni Klasör Oluştur"
                open={createFolderModalVisible}
                onCancel={() => {
                    setCreateFolderModalVisible(false);
                    createFolderForm.resetFields();
                }}
                footer={null}
            >
                <Form form={createFolderForm} onFinish={handleCreateFolder}>
                    <Form.Item
                        name="folderName"
                        label="Klasör Adı"
                        rules={[
                            { required: true, message: 'Lütfen klasör adını girin!' },
                            { min: 3, message: 'Klasör adı en az 3 karakter olmalıdır!' }
                        ]}
                    >
                        <Input placeholder="Klasör adını girin" />
                    </Form.Item>
                    <Form.Item>
                        <Space>
                            <Button type="primary" htmlType="submit" loading={loading}>
                                Oluştur
                            </Button>
                            <Button onClick={() => {
                                setCreateFolderModalVisible(false);
                                createFolderForm.resetFields();
                            }}>
                                İptal
                            </Button>
                        </Space>
                    </Form.Item>
                </Form>
            </Modal>

            {/* Paylaşılan Kullanıcılar Modalı */}
            <Modal
                title={`${selectedFileForSharedUsers?.name} Paylaşıldığı Kişiler`}
                open={sharedUsersModalVisible}
                onCancel={() => {
                    setSharedUsersModalVisible(false);
                    setSelectedFileForSharedUsers(null);
                    setFileSharedUsers([]); // Modalı kapatırken listeyi temizle
                }}
                footer={null} // Footer istemiyorsak null
                width={600}
            >
                 <Table
                    columns={[
                        { title: 'Kullanıcı Adı', dataIndex: 'username', key: 'username' },
                        { title: 'E-posta', dataIndex: 'email', key: 'email' },
                        { title: 'Paylaşım Tarihi', dataIndex: 'sharedAt', key: 'sharedAt', render: (date: string) => new Date(date).toLocaleString('tr-TR') },
                        {
                            title: 'İşlemler',
                            key: 'actions',
                            render: (_: any, record: any) => (
                                 <Button
                                     danger
                                     onClick={() => handleRevokeAccess(selectedFileForSharedUsers!.id, record.userId)}
                                 >
                                     Erişimi Kaldır
                                 </Button>
                            ),
                        },
                    ]}
                    dataSource={fileSharedUsers}
                    rowKey="userId"
                    loading={loadingSharedUsers}
                    pagination={false} // Paylaşılan kullanıcı sayısı az olacağı varsayımıyla sayfalama yok
                    locale={{
                        emptyText: 'Bu dosya henüz kimseyle paylaşılmamış.'
                    }}
                 />
            </Modal>
        </div>
    );
};

export default FileManager; 