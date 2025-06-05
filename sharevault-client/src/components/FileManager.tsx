import React, { useState, useEffect } from 'react';
import { Table, Button, Upload, message, Modal, Form, Input, Space, Card, Typography, Progress, Image, Tooltip, Dropdown, Select, Badge } from 'antd';
import { UploadOutlined, DownloadOutlined, ShareAltOutlined, DeleteOutlined, InboxOutlined, FileOutlined, IeOutlined, MoreOutlined, StopOutlined, TeamOutlined, HistoryOutlined, UploadOutlined as UploadOutlinedIcon } from '@ant-design/icons';
import type { UploadFile } from 'antd/es/upload/interface';
import fileService, { FileDto as FileServiceDto, FileVersionDto } from '../services/fileService';
import folderService from '../services/folderService';
import userService from '../services/userService';
import { useAuth } from '../contexts/AuthContext';
import { PermissionType } from '../types/folderTypes';

const { Title } = Typography;
const { Dragger } = Upload;

interface UploadProgressEvent {
    loaded: number;
    total: number;
}

interface SharedUser {
    userId: string;
    username: string;
    email: string;
    sharedAt: string;
}

// FileServiceDto'yu genişletmek için yeni bir tip tanımlıyoruz
type ExtendedFileDto = Omit<FileServiceDto, 'folderId'> & {
    folderId?: string | null;
    icon?: string;
    isPreviewable?: boolean;
    isFolder?: boolean;
};

const FileManager: React.FC = () => {
    const [files, setFiles] = useState<ExtendedFileDto[]>([]);
    const [sharedFiles, setSharedFiles] = useState<ExtendedFileDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [loadingSharedFiles, setLoadingSharedFiles] = useState(false);
    const [uploading, setUploading] = useState(false);
    const [uploadProgress, setUploadProgress] = useState(0);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [shareModalVisible, setShareModalVisible] = useState(false);
    const [selectedFileForShare, setSelectedFileForShare] = useState<ExtendedFileDto | null>(null);
    const [selectedItemForShare, setSelectedItemForShare] = useState<{ id: string, name: string, type: 'file' | 'folder' } | null>(null);
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

    // Paylaşım detayları için state'ler
    const [sharedUsersModalVisible, setSharedUsersModalVisible] = useState(false);
    const [selectedFileForSharedUsers, setSelectedFileForSharedUsers] = useState<ExtendedFileDto | null>(null);
    const [sharedUsers, setSharedUsers] = useState<SharedUser[]>([]);
    const [loadingSharedUsers, setLoadingSharedUsers] = useState(false);

    const [renameModalVisible, setRenameModalVisible] = useState(false);
    const [selectedFolder, setSelectedFolder] = useState<ExtendedFileDto | null>(null);
    const [renameForm] = Form.useForm();

    const [versionsModalVisible, setVersionsModalVisible] = useState(false);
    const [selectedFileForVersions, setSelectedFileForVersions] = useState<ExtendedFileDto | null>(null);
    const [fileVersions, setFileVersions] = useState<FileVersionDto[]>([]);
    const [loadingVersions, setLoadingVersions] = useState(false);
    const [newVersionModalVisible, setNewVersionModalVisible] = useState(false);
    const [selectedFileForNewVersion, setSelectedFileForNewVersion] = useState<ExtendedFileDto | null>(null);
    const [newVersionForm] = Form.useForm();

    const [uploadForm] = Form.useForm();

    useEffect(() => {
        if (user) {
            loadFiles();
            loadSharedFiles();
        }
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
            // FileServiceDto'yu ExtendedFileDto'ya dönüştürme
            const extendedFiles: ExtendedFileDto[] = fileList.map(file => ({
                ...file,
                icon: file.contentType === 'folder' ? '📁' : '📄',
                isPreviewable: file.contentType.startsWith('image/'),
                isFolder: file.contentType === 'folder'
            }));
            setFiles(extendedFiles);
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
            
            // Paylaşılan dosyaları yükle
            const sharedFileList = await fileService.getSharedFiles();
            const extendedSharedFiles: ExtendedFileDto[] = sharedFileList.map(file => ({
                ...file,
                icon: file.contentType === 'folder' ? '📁' : '📄',
                isPreviewable: file.contentType.startsWith('image/'),
                isFolder: file.contentType === 'folder'
            }));
            
            // Paylaşılan klasörleri yükle
            const sharedFolders = await folderService.getSharedFolders();
            const extendedSharedFolders: ExtendedFileDto[] = sharedFolders.map(folder => ({
                id: folder.id,
                name: folder.name,
                contentType: 'folder',
                size: 0,
                uploadedAt: folder.createdAt || new Date().toISOString(),
                uploadedBy: folder.createdByUsername || '',
                userId: folder.createdById || '',
                folderId: folder.parentFolderId,
                icon: '📁',
                isFolder: true
            }));
            
            // Dosya ve klasörleri birleştir
            setSharedFiles([...extendedSharedFiles, ...extendedSharedFolders]);
            console.log('Paylaşılan dosya ve klasörler yüklendi:', {
                dosya: extendedSharedFiles.length,
                klasör: extendedSharedFolders.length,
                toplam: extendedSharedFiles.length + extendedSharedFolders.length
            });
        } catch (error: any) {
            console.error('Paylaşılan öğeler yüklenirken hata:', error);
            message.error('Paylaşılan dosyalar ve klasörler yüklenirken bir hata oluştu: ' + error.message);
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
    };

    const handleCompleteUpload = async () => {
        if (!selectedFile || !tempFileName) return;

        try {
            // Önce aynı isimde dosya var mı kontrol et
            const existingFile = files.find(f => f.name === selectedFile.name && !f.isFolder);
            
            if (existingFile) {
                // Aynı isimde dosya varsa, not modalını göster
                setSelectedFileForNewVersion(existingFile);
                setNewVersionModalVisible(true);
                return;
            }

            // Aynı isimde dosya yoksa normal yükleme yap
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
                setFiles(files.filter((file: ExtendedFileDto) => file.id !== fileId));
            }
        }
    };

    const handleDelete = async (itemId: string, itemType: 'file' | 'folder') => {
        Modal.confirm({
            title: `${itemType === 'file' ? 'Dosyayı' : 'Klasörü'} silmek istediğinize emin misiniz?`,
            content: 'Bu işlem geri alınamaz.',
            okText: 'Evet',
            okType: 'danger',
            cancelText: 'Hayır',
            onOk: async () => {
                try {
                    setLoading(true);
                    if (itemType === 'file') {
                        await fileService.deleteFile(itemId);
                        message.success('Dosya başarıyla silindi');
                    } else {
                        await folderService.deleteFolder(itemId);
                        message.success('Klasör başarıyla silindi');
                    }
                    await loadFiles();
                } catch (error: any) {
                    message.error(`${itemType === 'file' ? 'Dosya' : 'Klasör'} silinirken bir hata oluştu: ` + (error.message || ''));
                    await loadFiles();
                } finally {
                    setLoading(false);
                }
            }
        });
    };

    const handleShare = async (values: { email: string, permission?: string }) => {
        if (!selectedItemForShare) return;

        try {
            setLoading(true);
            console.log('Paylaşım başlatılıyor:', selectedItemForShare);

            if (selectedItemForShare.type === 'folder') {
                // Klasör paylaşımı
                let permissionType: PermissionType;

                switch (values.permission) {
                    case 'Read':
                        permissionType = PermissionType.Read;
                        break;
                    case 'Write':
                        permissionType = PermissionType.Write;
                        break;
                    case 'Delete':
                        permissionType = PermissionType.Delete;
                        break;
                    case 'Share':
                        permissionType = PermissionType.Share;
                        break;
                    case 'FullControl':
                        permissionType = PermissionType.FullControl;
                        break;
                    default:
                        permissionType = PermissionType.Read;
                        break;
                }

                try {
                    // Önce e-posta adresinden kullanıcı ID'sini al
                    const userInfo = await userService.getUserByEmail(values.email);

                    console.log('Kullanıcı bilgileri alındı:', userInfo);
                    console.log('Klasör paylaşım bilgileri:', {
                        folderId: selectedItemForShare.id,
                        sharedWithUserId: userInfo.id,
                        permission: permissionType
                    });

                    // Backend'e kullanıcı ID'si ile paylaşım isteği gönder
                    await folderService.shareFolder(selectedItemForShare.id, {
                        sharedWithUserId: userInfo.id,
                        permission: permissionType
                    });
                    message.success('Klasör başarıyla paylaşıldı');
                } catch (userError: any) {
                    message.error(`Kullanıcı bulunamadı veya paylaşım işlemi başarısız oldu: ${userError.message}`);
                    throw userError; // Ana catch bloğuna ilet
                }
            } else {
                // Dosya paylaşımı
                console.log('Dosya paylaşım bilgileri:', {
                    FileId: selectedItemForShare.id,
                    UserEmails: [values.email]
                });

                await fileService.shareFile(selectedItemForShare.id, values.email);
                message.success('Dosya başarıyla paylaşıldı');
            }

            setShareModalVisible(false);
            setSelectedItemForShare(null);

        } catch (error: any) {
            message.error(`${selectedItemForShare.type === 'folder' ? 'Klasör' : 'Dosya'} paylaşılırken bir hata oluştu: ${error.message}`);
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
        const fileToPreview = files.find((f: ExtendedFileDto) => f.id === fileId);
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

    const parentFolderItem = files.find((item: ExtendedFileDto) => item.id === currentFolderId && item.contentType === 'folder');
    const parentFolderId = parentFolderItem?.folderId;

    const showRenameModal = (record: ExtendedFileDto) => {
        setSelectedFolder(record);
        renameForm.setFieldsValue({ newName: record.name });
        setRenameModalVisible(true);
    };

    const handleRenameSubmit = async () => {
        try {
            const values = await renameForm.validateFields();
            if (selectedFolder) {
                await handleRenameFolder(selectedFolder.id, values.newName);
                setRenameModalVisible(false);
                renameForm.resetFields();
            }
        } catch (error) {
            // Form validation failed
        }
    };

    const handleViewVersions = async (file: ExtendedFileDto) => {
        try {
            setSelectedFileForVersions(file);
            setLoadingVersions(true);
            const versions = await fileService.getFileVersions(file.id);
            console.log('Dosya versiyonları:', versions);
            setFileVersions(versions);
            setVersionsModalVisible(true);
        } catch (error: any) {
            message.error('Versiyonlar yüklenirken bir hata oluştu: ' + error.message);
        } finally {
            setLoadingVersions(false);
        }
    };

    const handleDownloadVersion = async (fileId: string, versionNumber: number, fileName: string) => {
        try {
            const blob = await fileService.downloadFileVersion(fileId, versionNumber);
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `${fileName} (v${versionNumber})`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
            message.success('Dosya versiyonu indirme başladı');
        } catch (error: any) {
            message.error('Dosya versiyonu indirilirken bir hata oluştu: ' + error.message);
        }
    };

    const columns = [
        {
            title: 'Ad',
            dataIndex: 'name',
            key: 'name',
            render: (text: string, record: ExtendedFileDto) => (
                <Space>
                    <span>{record.icon}</span>
                    <span>{text}</span>
                    {!record.isFolder && !!record.versionCount && record.versionCount > 0 && (
                        <Badge count={record.versionCount} style={{ backgroundColor: '#1890ff' }} />
                    )}
                </Space>
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
            render: (_: any, record: ExtendedFileDto) => (
                <Space>
                    {record.contentType === 'folder' ? (
                        <Dropdown key={`dropdown-${record.id}`}
                            menu={{
                                items: [
                                    {
                                        key: 'rename',
                                        label: 'Yeniden Adlandır',
                                        onClick: () => showRenameModal(record)
                                    },
                                    {
                                        key: 'share',
                                        label: 'Paylaş',
                                        icon: <ShareAltOutlined />,
                                        onClick: () => {
                                            if (record.userId === user?.id) {
                                                const folderRecord = { ...record, isFolder: true, contentType: 'folder' };
                                                handleShareClick(folderRecord);
                                            } else {
                                                message.info('Bu klasörü paylaşma yetkiniz yok.');
                                            }
                                        }
                                    },
                                    {
                                        key: 'shared-users',
                                        label: 'Paylaşım Detayları',
                                        icon: <TeamOutlined />,
                                        onClick: () => handleViewSharedUsers(record.id, record.name, true)
                                    },
                                    {
                                        key: 'delete',
                                        label: 'Sil',
                                        danger: true,
                                        onClick: () => handleDelete(record.id, 'folder')
                                    }
                                ]
                            }}
                        >
                            <Button icon={<MoreOutlined />} type="text" />
                        </Dropdown>
                    ) : (
                        <>
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
                            {!!record.versionCount && record.versionCount > 0 && (
                                <Tooltip title="Versiyonlar">
                                    <Button
                                        icon={<HistoryOutlined />}
                                        onClick={() => handleViewVersions(record)}
                                        type="text"
                                    />
                                </Tooltip>
                            )}
                            {record.userId === user?.id && (
                                <Tooltip title="Sil">
                                    <Button
                                        icon={<DeleteOutlined />}
                                        onClick={() => handleDelete(record.id, 'file')}
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
                                            handleShareClick(record);
                                        } else {
                                            message.info('Bu dosyayı paylaşma yetkiniz yok.');
                                        }
                                    }}
                                    type="text"
                                />
                            </Tooltip>
                            {record.userId === user?.id && record.contentType !== 'folder' && (
                                <Tooltip title="Yeni Versiyon Yükle">
                                    <Button
                                        icon={<HistoryOutlined />}
                                        onClick={() => handleNewVersion(record)}
                                        type="primary"
                                        size="small"
                                    />
                                </Tooltip>
                            )}
                            {record.userId === user?.id && record.contentType !== 'folder' && (
                                <Tooltip title="Paylaşım Detayları">
                                    <Button
                                        icon={<TeamOutlined />}
                                        onClick={() => handleViewSharedUsers(record.id, record.name, false)}
                                        type="text"
                                    />
                                </Tooltip>
                            )}
                        </>
                    )}
                </Space>
            ),
        },
    ];

    const sharedFilesColumns = [
        {
            title: 'Dosya Adı',
            dataIndex: 'name',
            key: 'name',
            render: (text: string, record: ExtendedFileDto) => (
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
            render: (_: any, record: ExtendedFileDto) => (
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
                                    handleViewSharedUsers(record.id, record.name, record.contentType === 'folder');
                                } else {
                                    // Başkasının sizinle paylaştığı dosyaysa yetki uyarısı ver
                                    message.info('Bu dosyanın paylaşım detaylarını görme yetkiniz yok.');
                                }
                            }}
                            type="text"
                        />
                    </Tooltip>
                    {/* Kullanıcı kendisiyle paylaşılan bir dosyaya erişimini iptal edebilir */}
                    {currentView === 'sharedFiles' && (
                        <Tooltip title="Erişimimi İptal Et">
                            <Button
                                icon={<StopOutlined />}
                                onClick={() => handleRemoveMyAccess(record.id, user?.id || '')}
                                type="text"
                                danger
                            />
                        </Tooltip>
                    )}
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
        onChange: (newSelectedRowKeys: React.Key[], selectedRows: ExtendedFileDto[]) => {
            setSelectedRowKeys(newSelectedRowKeys.map(key => key.toString()));
        },
    };

    const filteredFiles = files.filter((file: ExtendedFileDto) =>
        file.name.toLowerCase().includes(searchText.toLowerCase())
    );

    // Paylaşılan kullanıcıları görüntülemek için
    const handleViewSharedUsers = async (itemId: string, itemName: string, isFolder: boolean) => {
        try {
            console.log('Paylaşılan kullanıcıları görüntüleme isteği:', { itemId, itemName, isFolder });
            
            setSelectedFileForSharedUsers({
                id: itemId,
                name: itemName,
                isFolder: isFolder,
                // Diğer alanlar için varsayılan değerler
                contentType: '',
                size: 0,
                uploadedAt: '',
                uploadedBy: '',
                userId: ''
            });
            
            setLoadingSharedUsers(true);
            setSharedUsersModalVisible(true);
            
            try {
                let users;
                if (isFolder) {
                    users = await folderService.getSharedUsers(itemId);
                    console.log('Klasör için paylaşılan kullanıcılar API yanıtı:', users);
                } else {
                    const response = await fileService.getSharedUsers(itemId);
                    console.log('Dosya için paylaşılan kullanıcılar API yanıtı:', response);
                    users = response.sharedUsers || [];
                }
                
                console.log('İşlenmiş paylaşılan kullanıcılar:', users);
                
                if (Array.isArray(users)) {
                    setSharedUsers(users);
                } else {
                    console.error('Beklenmeyen API yanıtı:', users);
                    setSharedUsers([]);
                    message.error('Paylaşılan kullanıcılar alınırken bir hata oluştu');
                }
            } catch (error: any) {
                console.error('Paylaşılan kullanıcılar alınırken hata:', error);
                setSharedUsers([]);
                message.error(`Paylaşılan kullanıcılar alınırken bir hata oluştu: ${error.message}`);
            } finally {
                setLoadingSharedUsers(false);
            }
        } catch (error: any) {
            console.error('Paylaşılan kullanıcıları görüntüleme işlemi başlatılırken hata:', error);
            message.error(`İşlem başlatılamadı: ${error.message}`);
        }
    };

    // fileSharedUsers state'i değiştiğinde konsola yazdırmak için useEffect ekledim
    useEffect(() => {
        console.log('sharedUsers state güncellendi:', sharedUsers);
    }, [sharedUsers]);

    // Kullanıcının kendisine paylaşılan bir dosya/klasöre erişimini iptal etmesi için
    const handleRemoveMyAccess = async (itemId: string, userId: string) => {
        try {
            // Paylaşılan dosya/klasörü bul
            const item = sharedFiles.find(item => item.id === itemId);
            if (!item) {
                message.error('Dosya/klasör bulunamadı');
                return;
            }

            const isFolder = item.contentType === 'folder' || item.isFolder === true;
            const itemTypeText = isFolder ? 'klasöre' : 'dosyaya';

            Modal.confirm({
                title: 'Erişimi İptal Et',
                content: `Bu ${itemTypeText} erişiminizi iptal etmek istediğinizden emin misiniz?`,
                okText: 'Evet',
                okType: 'danger',
                cancelText: 'Hayır',
                onOk: async () => {
                    try {
                        setLoading(true);
                        
                        if (isFolder) {
                            // Klasör erişimini iptal et
                            await folderService.revokeAccess(itemId, userId);
                        } else {
                            // Dosya erişimini iptal et
                            await fileService.revokeAccess(itemId, userId);
                        }
                        
                        message.success(`${isFolder ? 'Klasör' : 'Dosya'} erişiminiz başarıyla iptal edildi`);
                        
                        // Paylaşılan dosya/klasör listesini güncelle
                        await loadSharedFiles();
                    } catch (error: any) {
                        console.error('Erişim iptali sırasında hata:', error);
                        message.error('Erişim iptali sırasında bir hata oluştu: ' + error.message);
                    } finally {
                        setLoading(false);
                    }
                }
            });
        } catch (error: any) {
            console.error('Erişim iptali işlemi sırasında hata:', error);
            message.error('Erişim iptali işlemi sırasında bir hata oluştu: ' + error.message);
        }
    };

    // Paylaşım erişimini kaldırmak için (dosya/klasör sahibi başkalarının erişimini kaldırır)
    const handleRevokeAccess = async (folderId: string, userId: string) => {
        try {
            console.log('Erişim iptali isteği:', { itemId: folderId, sharedWithUserId: userId });
            
            if (!selectedFileForSharedUsers) {
                console.error('handleRevokeAccess: Seçili dosya/klasör bulunamadı');
                message.error('Erişim iptali için seçili dosya/klasör bilgisi eksik');
                return;
            }
            
            // Konsolda görülen verilere göre, backend'e gönderilen record ID'sini kontrol edelim
            console.log('Seçilen klasör/dosya:', selectedFileForSharedUsers);
            
            Modal.confirm({
                title: 'Erişimi İptal Et',
                content: 'Bu kullanıcının erişimini iptal etmek istediğinizden emin misiniz?',
                okText: 'Evet',
                okType: 'danger',
                cancelText: 'Hayır',
                onOk: async () => {
                    try {
                        setLoadingSharedUsers(true);
                        
                        // isFolder değerine göre doğru servisi çağır
                        if (selectedFileForSharedUsers?.isFolder) {
                            console.log('Klasör erişimi iptal ediliyor. FolderId:', folderId, 'SharedWithUserId:', userId);
                            await folderService.revokeAccess(folderId, userId);
                        } else {
                            console.log('Dosya erişimi iptal ediliyor. FileId:', folderId, 'SharedWithUserId:', userId);
                            await fileService.revokeAccess(folderId, userId);
                        }
                        
                        message.success('Erişim başarıyla iptal edildi');
                        
                        // Paylaşım listesini güncelle
                        if (selectedFileForSharedUsers) {
                            await handleViewSharedUsers(
                                selectedFileForSharedUsers.id,
                                selectedFileForSharedUsers.name,
                                selectedFileForSharedUsers.isFolder || false
                            );
                        }
                        
                        // Paylaşılan dosyalar/klasörler listesini güncelle
                        if (currentView === 'sharedFiles') {
                            await loadSharedFiles();
                        }
                    } catch (error: any) {
                        console.error('Paylaşım erişimi kaldırılırken hata:', error);
                        
                        // Hata mesajını kullanıcıya göster
                        if (error.response?.status === 404) {
                            message.error('Aktif paylaşım bulunamadı. Kullanıcı ile klasör arasında aktif bir paylaşım olmayabilir.');
                        } else {
                            message.error(`Erişim iptal edilirken bir hata oluştu: ${error.message}`);
                        }
                    } finally {
                        setLoadingSharedUsers(false);
                    }
                }
            });
        } catch (error: any) {
            console.error('Erişim iptali işlemi başlatılırken hata:', error);
            message.error(`Erişim iptal işlemi başlatılamadı: ${error.message}`);
        }
    };

    const handleRenameFolder = async (folderId: string, newName: string) => {
        try {
            setLoading(true);
            await fileService.renameFolder(folderId, newName);
            message.success('Klasör başarıyla yeniden adlandırıldı');
            await loadFiles();
        } catch (error: any) {
            message.error('Klasör yeniden adlandırılırken bir hata oluştu: ' + error.message);
        } finally {
            setLoading(false);
        }
    };

    const handleShareClick = (item: ExtendedFileDto) => {
        console.log('handleShareClick called with item:', item);
        // contentType veya isFolder alanını kontrol ederek klasör mü dosya mı olduğunu belirle
        const isFolder = item.contentType === 'folder' || item.isFolder === true;

        setSelectedItemForShare({
            id: item.id,
            name: item.name,
            type: isFolder ? 'folder' : 'file'
        });
        
        console.log('selectedItemForShare set to:', {
            id: item.id,
            name: item.name,
            type: isFolder ? 'folder' : 'file'
        });
        
        setShareModalVisible(true);
    };

    const handleNewVersion = async (file: ExtendedFileDto) => {
        try {
            // Önce modalı açalım
            setSelectedFileForNewVersion(file);
            setNewVersionModalVisible(true);
            
            // Form'u sıfırlayalım
            newVersionForm.resetFields();
            
            // Önceki seçili dosyayı temizleyelim
            setSelectedFile(null);
            setPreviewImage('');
            setUploadProgress(0);
            setTempFileName(null);
        } catch (error: any) {
            message.error('Yeni versiyon oluşturma işlemi başlatılırken bir hata oluştu: ' + error.message);
        }
    };

    const handleNewVersionSubmit = async (values: { changeNotes: string }) => {
        if (!selectedFileForNewVersion || !selectedFile) {
            message.error('Lütfen bir dosya seçin');
            return;
        }

        try {
            // Dosya isimlerini karşılaştır
            const originalFileName = selectedFileForNewVersion.name;
            const newFileName = selectedFile.name;

            if (originalFileName !== newFileName) {
                // Farklı dosya ismi seçilmişse onay iste
                Modal.confirm({
                    title: 'Farklı Dosya İsmi',
                    content: `Orijinal dosya: ${originalFileName}\nSeçilen dosya: ${newFileName}\n\nFarklı bir dosya ismi seçtiniz. Yeni versiyon olarak yüklemek istediğinizden emin misiniz?`,
                    okText: 'Evet, Yükle',
                    cancelText: 'İptal',
                    onOk: async () => {
                        await uploadNewVersion(values.changeNotes);
                    }
                });
            } else {
                // Aynı dosya ismi ise direkt yükle
                await uploadNewVersion(values.changeNotes);
            }
        } catch (error: any) {
            message.error('Yeni versiyon oluşturulurken bir hata oluştu: ' + error.message);
        }
    };

    const uploadNewVersion = async (changeNotes: string) => {
        if (!selectedFile || !selectedFileForNewVersion) {
            message.error('Dosya bilgileri eksik');
            return;
        }

        try {
            setLoading(true);
            // Önce dosyayı yükle
            await fileService.uploadFile(selectedFile, currentFolderId);
            // Sonra versiyon notunu ekle
            await fileService.createFileVersion(selectedFileForNewVersion.id, changeNotes);
            
            message.success('Yeni versiyon başarıyla oluşturuldu');
            setNewVersionModalVisible(false);
            newVersionForm.resetFields();
            setSelectedFile(null);
            setPreviewImage('');
            setUploadProgress(0);
            setSelectedFileForNewVersion(null);
            await loadFiles();
        } catch (error: any) {
            message.error('Yeni versiyon oluşturulurken bir hata oluştu: ' + error.message);
        } finally {
            setLoading(false);
        }
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
                        <Form form={uploadForm} layout="vertical">
                            <Space key="upload-space" direction="vertical" style={{ width: '100%' }}>
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

                                {!uploading && selectedFileForNewVersion && (
                                    <Form.Item
                                        name="changeNotes"
                                        label="Değişiklik Notları"
                                        rules={[{ required: true, message: 'Lütfen değişiklik notlarını girin!' }]}
                                    >
                                        <Input.TextArea 
                                            placeholder="Bu versiyonda yapılan değişiklikleri açıklayın..."
                                            rows={3}
                                        />
                                    </Form.Item>
                                )}
                                
                                {!uploading ? (
                                    <Space key="upload-buttons">
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
                        </Form>
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
                title={`"${selectedItemForShare?.name}" ${selectedItemForShare?.type === 'folder' ? 'Klasörünü' : 'Dosyasını'} Paylaş`}
                open={shareModalVisible}
                onCancel={() => {
                    setShareModalVisible(false);
                    shareForm.resetFields();
                    setSelectedItemForShare(null);
                }}
                footer={null}
            >
                <Form
                    form={shareForm}
                    layout="vertical"
                    onFinish={handleShare}
                >
                    <Form.Item
                        name="email"
                        label="Kullanıcı E-postası"
                        rules={[
                            { required: true, message: 'Lütfen bir e-posta adresi girin!' },
                            { type: 'email', message: 'Geçerli bir e-posta adresi girin!' }
                        ]}
                    >
                        <Input placeholder="Paylaşılacak kullanıcının e-postası" />
                    </Form.Item>
                    
                    {selectedItemForShare?.type === 'folder' && (
                        <Form.Item
                            name="permission"
                            label="İzin Türü"
                            initialValue="Read"
                        >
                            <Select>
                                <Select.Option value="Read">Okuma</Select.Option>
                                <Select.Option value="Write">Yazma</Select.Option>
                                <Select.Option value="Delete">Silme</Select.Option>
                                <Select.Option value="Share">Paylaşım</Select.Option>
                                <Select.Option value="FullControl">Tam Kontrol</Select.Option>
                            </Select>
                        </Form.Item>
                    )}
                    
                    <Form.Item>
                        <Space>
                            <Button type="primary" htmlType="submit" loading={loading}>
                                Paylaş
                            </Button>
                            <Button onClick={() => {
                                setShareModalVisible(false);
                                shareForm.resetFields();
                                setSelectedItemForShare(null);
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
                title={`"${selectedFileForSharedUsers?.name}" ${selectedFileForSharedUsers?.isFolder ? 'Klasörünün' : 'Dosyasının'} Paylaşıldığı Kişiler`}
                open={sharedUsersModalVisible}
                onCancel={() => {
                    setSharedUsersModalVisible(false);
                    setSelectedFileForSharedUsers(null);
                    setSharedUsers([]); // Modalı kapatırken listeyi temizle
                }}
                footer={null} // Footer istemiyorsak null
                width={600}
            >
                {sharedUsers && sharedUsers.length > 0 ? (
                    <Table
                        columns={[
                            { 
                                title: 'Kullanıcı Adı', 
                                dataIndex: 'sharedWithUserName', 
                                key: 'username', 
                                render: (text: string, record: any) => {
                                    // Veri modeli refaktörü sonrası farklı alanları kontrol et
                                    // Öncelik sırası: sharedWithUserName > username > name
                                    const userName = record.sharedWithUserName || 
                                                    record.username || 
                                                    record.name ||
                                                    'Bilinmeyen Kullanıcı';
                                    
                                    // Kullanıcı ID'si için tüm olası alanları kontrol et
                                    const userId = record.sharedWithUserId || record.userId || (record as any).id || Math.random();
                                    
                                    return <span key={`username-${userId}`}>{userName}</span>;
                                }
                            },
                            { 
                                title: 'E-posta', 
                                dataIndex: 'sharedWithUserEmail', 
                                key: 'email', 
                                render: (text: string, record: any) => {
                                    // Veri modeli refaktörü sonrası farklı alanları kontrol et
                                    // Öncelik sırası: sharedWithUserEmail > email
                                    const email = record.sharedWithUserEmail || 
                                                 record.email || 
                                                 '';
                                    
                                    // Kullanıcı ID'si için tüm olası alanları kontrol et
                                    const userId = record.sharedWithUserId || record.userId || (record as any).id || Math.random();
                                    
                                    return <span key={`email-${userId}`}>{email}</span>;
                                }
                            },
                            { 
                                title: 'Paylaşım Tarihi', 
                                dataIndex: 'sharedAt', 
                                key: 'sharedAt', 
                                render: (date: string, record: any) => {
                                    const dateStr = date ? new Date(date).toLocaleString('tr-TR') : 'Belirtilmemiş';
                                    return <span key={`date-${record.sharedWithUserId || Math.random()}`}>{dateStr}</span>;
                                }
                            },
                            {
                                title: 'İşlemler',
                                key: 'actions',
                                render: (_, record) => {
                                    // Debug log kullanıcı bilgilerini göster
                                    // Tüm olasi kullanıcı ID alanlarını inceleyelim
                                    console.log('Render edilen kullanıcı:', record);
                                    
                                    // Record içindeki tüm ID benzeri alanları loglayarak kontrol edelim
                                    const allFields = Object.keys(record).filter(key => 
                                        key.toLowerCase().includes('id') || 
                                        key.toLowerCase().includes('user')
                                    );
                                    
                                    console.log('Olası ID alanları:', allFields.map(field => ({
                                        field,
                                        value: (record as any)[field]
                                    })));
                                    
                                    // Kullanıcı veri modeli
                                    // Backend'den gelen veri ile en iyi eşleşen alanı bulmaya çalışıyoruz
                                    
                                    // Öncelik sırası ile kullanıcı ID alanlarını kontrol edelim
                                    // GUID kontrolü için regex - bu doğru kullanıcı ID'sini bulmamıza yardımcı olacak
                                    const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
                                    
                                    // Önce doğrudan backend model alanlarını kontrol edelim
                                    let userId = (record as any).sharedWithUserId;
                                    if (userId) {
                                        console.log('sharedWithUserId alanı bulundu:', userId);
                                    }
                                    
                                    // Eğer bu alanlarda kullanıcı ID bulunamadıysa, alternatif alanlara bakalım
                                    if (!userId || !guidRegex.test(userId)) {
                                        // Record nesnesi içindeki id alanı
                                        if ((record as any).id && guidRegex.test((record as any).id)) {
                                            userId = (record as any).id;
                                            console.log('ID alanı kullanıldı:', userId);
                                        }
                                        // userId alanı
                                        else if ((record as any).userId && guidRegex.test((record as any).userId)) {
                                            userId = (record as any).userId;
                                            console.log('userId alanı kullanıldı:', userId);
                                        }
                                        // SharedWithUserId alanı (büyük harfle başlayan)
                                        else if ((record as any).SharedWithUserId && guidRegex.test((record as any).SharedWithUserId)) {
                                            userId = (record as any).SharedWithUserId;
                                            console.log('SharedWithUserId alanı kullanıldı:', userId);
                                        }
                                    }
                                    
                                    console.log('Tespit edilen kullanıcı ID:', userId);
                                    
                                    return (
                                        <Button
                                            key={`revoke-${userId}`}
                                            type="primary"
                                            danger
                                            icon={<StopOutlined />}
                                            onClick={() => {
                                                // Veri modeli refaktörü sonrası backend DTO'larındaki alan adlarını kullanmalıyız
                                                // Öncelikle sharedWithUserId alanını kontrol edelim, yoksa diğer alanlara bakalım
                                                const sharedWithUserId = (record as any).sharedWithUserId || 
                                                                      (record as any).userId || 
                                                                      userId; // Son çare olarak render fonksiyonuna gelen userId'yi kullan
                                                
                                                console.log('Erişim iptal edilecek kullanıcı kaydı:', record);
                                                console.log('Tespit edilen kullanıcı ID:', sharedWithUserId);
                                                
                                                // Kullanıcı ID'si bulunamadıysa hata göster
                                                if (!sharedWithUserId) {
                                                    message.error('Kullanıcı ID bilgisi bulunamadı');
                                                    console.error('Geçersiz veri formatı. Kullanıcı kaydı:', JSON.stringify(record));
                                                    return;
                                                }
                                                
                                                // Klasör ID'si ve Paylaşılan Kullanıcı ID'si doğru bir şekilde gönderiliyor
                                                const itemId = selectedFileForSharedUsers!.id;
                                                console.log('Erişim iptali için gönderilen bilgiler:', { 
                                                    itemId, 
                                                    sharedWithUserId,
                                                    isFolder: selectedFileForSharedUsers!.isFolder
                                                });
                                                
                                                // Doğru parametre sırasıyla çağrı yapılıyor
                                                handleRevokeAccess(itemId, sharedWithUserId);
                                            }}
                                        >
                                            Erişimi İptal Et
                                        </Button>
                                    );
                                },
                            }
                        ]}
                        dataSource={sharedUsers}
                        rowKey={(record) => {
                            // Öncelikle sharedWithUserId, sonra userId, sonra id alanlarını kontrol et
                            return (record as any).sharedWithUserId || 
                                   (record as any).userId || 
                                   (record as any).id || 
                                   Math.random().toString(); // Son çare olarak rastgele bir değer kullan
                        }}
                        loading={loadingSharedUsers}
                        pagination={false}
                    />
                ) : (
                    <div style={{ textAlign: 'center', padding: '20px' }}>
                        {loadingSharedUsers ? (
                            <p>Yükleniyor...</p>
                        ) : (
                            <p>Bu dosya henüz kimseyle paylaşılmamış.</p>
                        )}
                    </div>
                )}
            </Modal>
            
            <Modal
                title="Klasörü Yeniden Adlandır"
                open={renameModalVisible}
                onOk={handleRenameSubmit}
                onCancel={() => {
                    setRenameModalVisible(false);
                    renameForm.resetFields();
                }}
                okText="Yeniden Adlandır"
                cancelText="İptal"
            >
                <Form form={renameForm}>
                    <Form.Item
                        name="newName"
                        rules={[{ required: true, message: 'Lütfen yeni klasör adını girin!' }]}
                    >
                        <Input />
                    </Form.Item>
                </Form>
            </Modal>

            {/* Versiyonlar Modalı */}
            <Modal
                title={`${selectedFileForVersions?.name} - Versiyonlar`}
                open={versionsModalVisible}
                onCancel={() => {
                    setVersionsModalVisible(false);
                    setSelectedFileForVersions(null);
                    setFileVersions([]);
                }}
                footer={null}
                width={800}
            >
                <Table
                    dataSource={fileVersions}
                    loading={loadingVersions}
                    rowKey="id"
                    columns={[
                        {
                            title: 'Versiyon',
                            dataIndex: 'versionNumber',
                            key: 'versionNumber',
                            render: (versionNumber: number) => `v${versionNumber}`,
                        },
                        {
                            title: 'Yüklenme Tarihi',
                            dataIndex: 'uploadedAt',
                            key: 'uploadedAt',
                            render: (date: string) => new Date(date).toLocaleString('tr-TR'),
                        },
                        {
                            title: 'Boyut',
                            dataIndex: 'fileSize',
                            key: 'fileSize',
                            render: (size: number) => formatFileSize(size),
                        },
                        {
                            title: 'Yükleyen',
                            dataIndex: 'uploadedBy',
                            key: 'uploadedBy',
                        },
                        {
                            title: 'Değişiklik Notları',
                            dataIndex: 'changeNotes',
                            key: 'changeNotes',
                            render: (notes: string) => notes || '-',
                        },
                        {
                            title: 'İşlemler',
                            key: 'actions',
                            render: (text: string, record: FileVersionDto) => (
                                <Space>
                                    <Tooltip title="İndir">
                                        <Button
                                            icon={<DownloadOutlined />}
                                            onClick={() => handleDownloadVersion(
                                                selectedFileForVersions?.id || '',
                                                record.versionNumber,
                                                selectedFileForVersions?.name || ''
                                            )}
                                            type="text"
                                        />
                                    </Tooltip>
                                </Space>
                            ),
                        },
                    ]}
                    pagination={false}
                />
            </Modal>

            <Modal
                title={`${selectedFileForNewVersion?.name} - Yeni Versiyon Yükle`}
                open={newVersionModalVisible}
                onCancel={() => {
                    setNewVersionModalVisible(false);
                    newVersionForm.resetFields();
                    setSelectedFile(null);
                    setPreviewImage('');
                    setUploadProgress(0);
                }}
                footer={null}
                width={600}
            >
                <Form form={newVersionForm} onFinish={handleNewVersionSubmit} layout="vertical">
                    <Form.Item
                        label="Dosya Seçin"
                        required
                        tooltip="Yeni versiyon olarak yüklenecek dosyayı seçin"
                    >
                        <Upload
                            beforeUpload={(file) => {
                                setSelectedFile(file);
                                return false;
                            }}
                            maxCount={1}
                            onRemove={() => {
                                setSelectedFile(null);
                                setPreviewImage('');
                            }}
                        >
                            <Button icon={<UploadOutlined />}>Dosya Seç</Button>
                        </Upload>
                    </Form.Item>

                    {selectedFile && (
                        <Form.Item
                            label="Seçilen Dosya"
                            tooltip="Yeni versiyon olarak yüklenecek dosya"
                        >
                            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                                <FileOutlined />
                                <span>{selectedFile.name}</span>
                                <span style={{ color: '#999' }}>
                                    ({formatFileSize(selectedFile.size)})
                                </span>
                            </div>
                        </Form.Item>
                    )}

                    <Form.Item
                        name="changeNotes"
                        label="Değişiklik Notları"
                        rules={[
                            { required: true, message: 'Lütfen değişiklik notlarını girin!' },
                            { min: 10, message: 'Değişiklik notları en az 10 karakter olmalıdır!' }
                        ]}
                        tooltip="Bu versiyonda yapılan değişiklikleri detaylı olarak açıklayın"
                    >
                        <Input.TextArea 
                            placeholder="Bu versiyonda yapılan değişiklikleri detaylı olarak açıklayın..."
                            rows={4}
                        />
                    </Form.Item>

                    {uploadProgress > 0 && (
                        <Progress percent={uploadProgress} status="active" />
                    )}

                    <Form.Item>
                        <Space>
                            <Button 
                                type="primary" 
                                htmlType="submit" 
                                loading={loading}
                                disabled={!selectedFile}
                            >
                                Yeni Versiyon Oluştur
                            </Button>
                            <Button 
                                onClick={() => {
                                    setNewVersionModalVisible(false);
                                    newVersionForm.resetFields();
                                    setSelectedFile(null);
                                    setPreviewImage('');
                                    setUploadProgress(0);
                                }}
                            >
                                İptal
                            </Button>
                        </Space>
                    </Form.Item>
                </Form>
            </Modal>
        </div>
    );
};

export default FileManager;
