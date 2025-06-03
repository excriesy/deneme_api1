import React, { useState, useEffect } from 'react';
import { Table, Button, message, Modal, Form, Input, Space, Card, Typography, Tooltip, Dropdown, Select } from 'antd';
import { 
    FolderOutlined, 
    ShareAltOutlined, 
    DeleteOutlined, 
    MoreOutlined, 
    StopOutlined, 
    TeamOutlined,
    EditOutlined,
    PlusOutlined
} from '@ant-design/icons';
import folderService from '../services/folderService';
import userService from '../services/userService';
import { useAuth } from '../contexts/AuthContext';
import { PermissionType } from '../types/folderTypes';

const { Title } = Typography;

// Genişletilmiş SharedUser arayüzü, dinamik erişim için index signature içeriyor
interface SharedUser {
    userId: string;
    username: string;
    email: string;
    sharedAt: string;
    sharedWithUserId?: string;
    id?: string;
    [key: string]: any; // Dinamik alan erişimi için index signature
}

// Klasör veri modeli
interface FolderDto {
    id: string;
    name: string;
    parentFolderId?: string | null;
    createdAt?: string;
    createdById?: string;
    createdByUsername?: string;
    isFolder: boolean;
    contentType: string;
    icon?: string;
}

interface FolderManagerProps {
    refreshTrigger?: number;
    onRefreshNeeded?: () => void;
}

const FolderManager: React.FC<FolderManagerProps> = ({ refreshTrigger = 0, onRefreshNeeded }) => {
    const [folders, setFolders] = useState<FolderDto[]>([]);
    const [sharedFolders, setSharedFolders] = useState<FolderDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [loadingSharedFolders, setLoadingSharedFolders] = useState(false);
    const [currentFolderId, setCurrentFolderId] = useState<string | null>(null);
    const [createFolderModalVisible, setCreateFolderModalVisible] = useState(false);
    const [createFolderForm] = Form.useForm();
    const [searchText, setSearchText] = useState('');
    const [currentView, setCurrentView] = useState<'myFolders' | 'sharedFolders'>('myFolders');

    // Paylaşım detayları için state'ler
    const [sharedUsersModalVisible, setSharedUsersModalVisible] = useState(false);
    const [selectedFolderForSharedUsers, setSelectedFolderForSharedUsers] = useState<FolderDto | null>(null);
    const [sharedUsers, setSharedUsers] = useState<SharedUser[]>([]);
    const [loadingSharedUsers, setLoadingSharedUsers] = useState(false);

    const [renameModalVisible, setRenameModalVisible] = useState(false);
    const [selectedFolder, setSelectedFolder] = useState<FolderDto | null>(null);
    const [renameForm] = Form.useForm();
    
    // Paylaşım modal'ı için state'ler
    const [shareModalVisible, setShareModalVisible] = useState(false);
    const [selectedFolderForShare, setSelectedFolderForShare] = useState<FolderDto | null>(null);
    const [shareForm] = Form.useForm();

    const { user } = useAuth();

    useEffect(() => {
        if (user) {
            loadFolders();
            loadSharedFolders();
        }
    }, [user]);
    
    // refreshTrigger değiştiğinde klasör listesini yenile
    useEffect(() => {
        if (user && refreshTrigger > 0) {
            loadFolders();
            loadSharedFolders();
        }
    }, [refreshTrigger]);

    useEffect(() => {
        if (currentView === 'myFolders') {
            loadFolders();
        }
    }, [currentFolderId, currentView]);

    // Klasörleri yükleme fonksiyonu
    const loadFolders = async () => {
        try {
            console.log('loadFolders başlatıldı. Mevcut folderId:', currentFolderId);
            setLoading(true);
            // currentFolderId string | null olabilir, getFolders bunu kabul ediyor
            // TypeScript hatasını önlemek için undefined'a dönüştürüyoruz
            const folderList = await folderService.getFolders(currentFolderId || undefined);
            console.log('loadFolders: Klasör listesi backend\'den alındı:', folderList);
            
            // FolderDto'ya dönüştürme
            const mappedFolders: FolderDto[] = folderList.map((folder: any) => ({
                ...folder,
                contentType: 'folder',
                isFolder: true,
                icon: '📁'
            }));
            
            setFolders(mappedFolders);
            console.log('loadFolders: Klasör listesi state\'e ayarlandı.');
        } catch (error: any) {
            console.error('Klasörler yüklenirken hata:', error);
            message.error('Klasörler yüklenirken bir hata oluştu: ' + error.message);
            setFolders([]);
        } finally {
            setLoading(false);
            console.log('loadFolders tamamlandı.');
        }
    };

    // Paylaşılan klasörleri yükleme fonksiyonu
    const loadSharedFolders = async () => {
        try {
            setLoadingSharedFolders(true);
            
            // Paylaşılan klasörleri yükle
            const sharedFoldersList = await folderService.getSharedFolders();
            const mappedSharedFolders: FolderDto[] = sharedFoldersList.map(folder => ({
                id: folder.id,
                name: folder.name,
                contentType: 'folder',
                parentFolderId: folder.parentFolderId,
                createdAt: folder.createdAt || new Date().toISOString(),
                createdById: folder.createdById || '',
                createdByUsername: folder.createdByUsername || '',
                isFolder: true,
                icon: '📁'
            }));
            
            setSharedFolders(mappedSharedFolders);
            console.log('Paylaşılan klasörler yüklendi:', {
                klasör: mappedSharedFolders.length
            });
        } catch (error: any) {
            console.error('Paylaşılan klasörler yüklenirken hata:', error);
            message.error('Paylaşılan klasörler yüklenirken bir hata oluştu: ' + error.message);
            setSharedFolders([]);
        } finally {
            setLoadingSharedFolders(false);
        }
    };

    // Klasör oluşturma fonksiyonu
    const handleCreateFolder = async (values: { folderName: string }) => {
        try {
            setLoading(true);
            console.log('Klasör oluşturuluyor:', values.folderName, 'Üst klasör:', currentFolderId);
            // null değerini undefined'a dönüştürerek TypeScript hatasını önlüyoruz
            await folderService.createFolder(values.folderName, currentFolderId || undefined);
            message.success(`"${values.folderName}" klasörü başarıyla oluşturuldu`);
            setCreateFolderModalVisible(false);
            createFolderForm.resetFields();
            await loadFolders();
            
            // FileManager'ı da güncellemek için onRefreshNeeded fonksiyonunu çağır
            if (onRefreshNeeded) {
                onRefreshNeeded();
            }
        } catch (error: any) {
            console.error('Klasör oluşturma hatası:', error);
            message.error(`Klasör oluşturulurken bir hata oluştu: ${error.message}`);
        } finally {
            setLoading(false);
        }
    };

    // Klasör yeniden adlandırma fonksiyonu
    const handleRenameFolder = async (folderId: string, newName: string) => {
        try {
            setLoading(true);
            console.log('Klasör yeniden adlandırılıyor:', folderId, newName);
            await folderService.renameFolder(folderId, newName);
            message.success(`Klasör başarıyla "${newName}" olarak yeniden adlandırıldı`);
            setRenameModalVisible(false);
            renameForm.resetFields();
            await loadFolders();
            
            // FileManager'ı da güncellemek için onRefreshNeeded fonksiyonunu çağır
            if (onRefreshNeeded) {
                onRefreshNeeded();
            }
        } catch (error: any) {
            console.error('Klasör yeniden adlandırma hatası:', error);
            message.error(`Klasör yeniden adlandırılırken bir hata oluştu: ${error.message}`);
        } finally {
            setLoading(false);
        }
    };

    // Klasör silme fonksiyonu
    const handleDeleteFolder = async (folderId: string) => {
        if (!folderId) {
            console.error('handleDeleteFolder: Geçersiz ID', { folderId });
            message.error('Silme işlemi için geçerli bir klasör ID gerekli');
            return;
        }

        console.log('Klasör silme işlemi başlatılıyor:', { folderId });

        Modal.confirm({
            title: 'Klasörü Sil',
            content: 'Bu klasörü ve içindeki tüm dosyaları silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.',
            okText: 'Evet, Sil',
            okType: 'danger',
            cancelText: 'İptal',
            onOk: async () => {
                try {
                    setLoading(true);
                    
                    console.log('Klasör siliniyor:', folderId);
                    await folderService.deleteFolder(folderId);
                    message.success('Klasör başarıyla silindi');
                    
                    // Klasör listesini yenile
                    await loadFolders();
                    
                    // Eğer paylaşılan klasörler görünümündeysek, o listeyi de güncelleyelim
                    if (currentView === 'sharedFolders') {
                        await loadSharedFolders();
                    }
                    
                    // FileManager'ı da güncellemek için onRefreshNeeded fonksiyonunu çağır
                    if (onRefreshNeeded) {
                        onRefreshNeeded();
                    }
                } catch (error: any) {
                    console.error('Klasör silme hatası:', error);
                    
                    let errorMessage = 'Klasör silinirken bir hata oluştu';
                    
                    // Hata detaylarını kontrol et
                    if (error.response) {
                        if (error.response.status === 403) {
                            errorMessage = 'Bu klasörü silme yetkiniz yok';
                        } else if (error.response.status === 404) {
                            errorMessage = 'Klasör bulunamadı';
                        } else if (error.response.data && error.response.data.message) {
                            errorMessage += `: ${error.response.data.message}`;
                        } else {
                            errorMessage += `: ${error.message}`;
                        }
                    } else {
                        errorMessage += `: ${error.message}`;
                    }
                    
                    message.error(errorMessage);
                    // Hata olsa da klasör listesini yenile
                    await loadFolders();
                } finally {
                    setLoading(false);
                }
            }
        });
    };

    // Klasör erişimini iptal etme fonksiyonu - bu fonksiyon daha aşağıda tanımlandığı için kaldırıldı

    // Klasör paylaşım fonksiyonu
    const handleShareFolder = async (values: { email: string, permission?: string }) => {
        if (!selectedFolderForShare) return;

        try {
            setLoading(true);
            console.log('Klasör paylaşımı başlatılıyor:', selectedFolderForShare);

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
            }

            // E-posta ve izin türü ile paylaşım yap
            await folderService.shareFolder(selectedFolderForShare.id, {
                sharedWithUserId: values.email,
                permission: permissionType
            });
            message.success(`Klasör başarıyla ${values.email} ile paylaşıldı`);
            setShareModalVisible(false);
            shareForm.resetFields();
            
            // FileManager'ı da güncellemek için onRefreshNeeded fonksiyonunu çağır
            if (onRefreshNeeded) {
                onRefreshNeeded();
            }
        } catch (error: any) {
            console.error('Klasör paylaşım hatası:', error);
            
            let errorMessage = 'Klasör paylaşılırken bir hata oluştu';
            
            if (error.response && error.response.data) {
                if (error.response.status === 404) {
                    errorMessage = 'Kullanıcı bulunamadı';
                } else if (error.response.status === 400) {
                    errorMessage = error.response.data.message || 'Geçersiz istek';
                } else {
                    errorMessage += `: ${error.response.data.message || error.message}`;
                }
            } else {
                errorMessage += `: ${error.message}`;
            }
            
            message.error(errorMessage);
        } finally {
            setLoading(false);
        }
    };

    // Paylaşım detaylarını görüntüleme fonksiyonu
    const handleViewSharedUsers = async (folderId: string, folderName: string) => {
        try {
            setLoadingSharedUsers(true);
            console.log('Paylaşılan kullanıcıları görüntüleme isteği:', { itemId: folderId, itemName: folderName, isFolder: true });
            
            // Seçili klasörü ayarla
            const folderForSharedUsers: FolderDto = {
                id: folderId,
                name: folderName,
                isFolder: true,
                contentType: 'folder'
            };
            setSelectedFolderForSharedUsers(folderForSharedUsers);
            
            // Klasör için paylaşılan kullanıcıları getir
            const sharedUsersData = await folderService.getSharedUsers(folderId);
            console.log('Klasör için paylaşılan kullanıcılar API yanıtı:', sharedUsersData);
            
            // Veri modelini düzenle
            const processedUsers = sharedUsersData.map((user: SharedUser) => ({
                ...user,
                sharedAt: user.sharedAt || new Date().toISOString()
            }));
            console.log('İşlenmiş paylaşılan kullanıcılar:', processedUsers);
            
            setSharedUsers(processedUsers);
            console.log('sharedUsers state güncellendi:', processedUsers);
            setSharedUsersModalVisible(true);
        } catch (error: any) {
            console.error('Paylaşılan kullanıcılar yüklenirken hata:', error);
            message.error(`Paylaşım detayları yüklenirken bir hata oluştu: ${error.message}`);
            setSharedUsers([]);
        } finally {
            setLoadingSharedUsers(false);
        }
    };

    // Paylaşım erişimini kaldırmak için (klasör sahibi başkalarının erişimini kaldırır)
    const handleRevokeAccess = async (sharingRecordId: string, userId: string) => {
        try {
            console.log('Erişim iptali isteği başlatılıyor:', { sharingRecordId, userId });
            
            if (!selectedFolderForSharedUsers) {
                console.error('handleRevokeAccess: Seçili klasör bulunamadı');
                message.error('Erişim iptali için seçili klasör bilgisi eksik');
                return;
            }
            
            // Seçili klasörün ID'si ve paylaşım yapılan kullanıcı ID'si
            const folderId = selectedFolderForSharedUsers.id;
            // userId burada sharedWithUserId olarak kullanılıyor - paylaşımın yapıldığı kullanıcı ID'si
            const sharedWithUserId = userId;
            console.log('Erişim iptali için kullanılacak bilgiler:', { 
                folderId,
                sharingRecordId, 
                sharedWithUserId
            });
            
            Modal.confirm({
                title: 'Erişimi İptal Et',
                content: 'Bu kullanıcının erişimini iptal etmek istediğinizden emin misiniz?',
                okText: 'Evet',
                okType: 'danger',
                cancelText: 'Hayır',
                onOk: async () => {
                    try {
                        setLoadingSharedUsers(true);
                        
                        console.log('Klasör erişimi iptal ediliyor:', {
                            folderId: folderId,
                            sharedWithUserId: sharedWithUserId
                        });
                        
                        // folderService.revokeAccess'e folderId ve sharedWithUserId parametrelerini geçir
                        // API endpoint: /folder/{folderId}/revoke-access - POST payload içinde SharedWithUserId bekliyor
                        await folderService.revokeAccess(folderId, sharedWithUserId);
                        
                        message.success('Erişim başarıyla iptal edildi');
                        
                        // API çağrısı başarılı olduktan sonra UI'ı hemen güncelle
                        // Revoke edilen paylaşım kaydını sharedUsers listesinden çıkar
                        setSharedUsers(prevUsers => {
                            // Paylaşım kaydı ID'si ile eşleşen kaydı filtrele
                            const updatedUsers = prevUsers.filter(user => {
                                // Paylaşım kaydı ID'si ile karşılaştır
                                return user.id !== sharingRecordId;
                            });
                            console.log('Güncellenmiş paylaşılan kullanıcılar listesi:', updatedUsers);
                            return updatedUsers;
                        });
                        
                        // Klasör listesini yenile
                        await loadFolders();
                        
                        // Paylaşılan klasörler listesini güncelle
                        if (currentView === 'sharedFolders') {
                            await loadSharedFolders();
                        }
                        
                        // FileManager'ı da güncellemek için onRefreshNeeded fonksiyonunu çağır
                        if (onRefreshNeeded) {
                            onRefreshNeeded();
                        }
                    } catch (error: any) {
                        console.error('Paylaşım erişimi kaldırılırken hata:', error);
                        
                        // Hata mesajını kullanıcıya göster
                        if (error.response?.status === 404) {
                            message.error('Aktif paylaşım bulunamadı. Kullanıcı ile klasör arasında aktif bir paylaşım olmayabilir.');
                        } else if (error.response?.status === 403) {
                            message.error('Bu işlem için yetkiniz bulunmamaktadır. Sadece klasör sahibi erişim iptal edebilir.');
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
    
    // Klasör paylaşım modal'ını açma fonksiyonu
    const handleShareClick = (folder: FolderDto) => {
        setSelectedFolderForShare(folder);
        setShareModalVisible(true);
        shareForm.resetFields();
    };

    // Klasör yeniden adlandırma modal'ını açma fonksiyonu
    const showRenameModal = (folder: FolderDto) => {
        setSelectedFolder(folder);
        setRenameModalVisible(true);
        renameForm.setFieldsValue({ folderName: folder.name });
    };
    
    // Klasör oluşturma modal'ını açma fonksiyonu
    const showCreateFolderModal = () => {
        setCreateFolderModalVisible(true);
        createFolderForm.resetFields();
    };

    // Klasör listesi için sütun tanımları
    const folderColumns = [
        {
            title: 'Klasör Adı',
            dataIndex: 'name',
            key: 'name',
            render: (text: string, record: FolderDto) => (
                <Space>
                    <FolderOutlined style={{ color: '#faad14' }} />
                    <span 
                        style={{ cursor: 'pointer', color: '#1890ff' }}
                        onClick={() => setCurrentFolderId(record.id)}
                    >
                        {text}
                    </span>
                </Space>
            ),
        },
        {
            title: 'Oluşturulma Tarihi',
            dataIndex: 'createdAt',
            key: 'createdAt',
            render: (text: string) => text ? new Date(text).toLocaleString() : '-',
        },
        {
            title: 'Oluşturan',
            dataIndex: 'createdByUsername',
            key: 'createdByUsername',
            render: (text: string) => text || '-',
        },
        {
            title: 'İşlemler',
            key: 'action',
            render: (text: string, record: FolderDto) => (
                <Space size="middle">
                    {record.createdById === user?.id ? (
                        <Dropdown
                            menu={{
                                items: [
                                    {
                                        key: 'rename',
                                        label: 'Yeniden Adlandır',
                                        icon: <EditOutlined />,
                                        onClick: () => showRenameModal(record)
                                    },
                                    {
                                        key: 'share',
                                        label: 'Paylaş',
                                        icon: <ShareAltOutlined />,
                                        onClick: () => handleShareClick(record)
                                    },
                                    {
                                        key: 'shared-users',
                                        label: 'Paylaşım Detayları',
                                        icon: <TeamOutlined />,
                                        onClick: () => {
                                            console.log('Klasör paylaşım detayları görüntüleniyor:', record.id, record.name);
                                            handleViewSharedUsers(record.id, record.name);
                                        }
                                    },
                                    {
                                        key: 'delete',
                                        label: 'Sil',
                                        danger: true,
                                        onClick: () => handleDeleteFolder(record.id)
                                    }
                                ]
                            }}
                        >
                            <Button icon={<MoreOutlined />} type="text" />
                        </Dropdown>
                    ) : (
                        <>
                            <Tooltip title="Paylaşım Detayları">
                                <Button
                                    icon={<TeamOutlined />}
                                    onClick={() => handleViewSharedUsers(record.id, record.name)}
                                    type="text"
                                />
                            </Tooltip>
                        </>
                    )}
                </Space>
            ),
        },
    ];

    return (
        <div style={{ padding: '20px' }}>
            <div style={{ marginBottom: '20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Title level={2}>
                    {currentView === 'myFolders' ? 'Klasörlerim' : 'Benimle Paylaşılan Klasörler'}
                </Title>
                <Space>
                    <Button 
                        type={currentView === 'myFolders' ? 'primary' : 'default'}
                        onClick={() => setCurrentView('myFolders')}
                    >
                        Klasörlerim
                    </Button>
                    <Button 
                        type={currentView === 'sharedFolders' ? 'primary' : 'default'}
                        onClick={() => setCurrentView('sharedFolders')}
                    >
                        Paylaşılan Klasörler
                    </Button>
                    {currentView === 'myFolders' && (
                        <Button 
                            type="primary" 
                            icon={<PlusOutlined />}
                            onClick={showCreateFolderModal}
                        >
                            Yeni Klasör
                        </Button>
                    )}
                </Space>
            </div>

            {currentView === 'myFolders' && (
                <Card>
                    <Table 
                        columns={folderColumns} 
                        dataSource={folders}
                        rowKey="id"
                        loading={loading}
                    />
                </Card>
            )}

            {currentView === 'sharedFolders' && (
                <Card>
                    <Table 
                        columns={folderColumns} 
                        dataSource={sharedFolders}
                        rowKey="id"
                        loading={loadingSharedFolders}
                    />
                </Card>
            )}

            {/* Klasör Oluşturma Modal'ı */}
            <Modal
                title="Yeni Klasör"
                open={createFolderModalVisible}
                onCancel={() => setCreateFolderModalVisible(false)}
                footer={null}
            >
                <Form
                    form={createFolderForm}
                    layout="vertical"
                    onFinish={handleCreateFolder}
                >
                    <Form.Item
                        name="folderName"
                        label="Klasör Adı"
                        rules={[{ required: true, message: 'Lütfen klasör adı girin' }]}
                    >
                        <Input placeholder="Klasör adı girin" />
                    </Form.Item>
                    <Form.Item>
                        <Button type="primary" htmlType="submit" loading={loading}>
                            Oluştur
                        </Button>
                    </Form.Item>
                </Form>
            </Modal>

            {/* Klasör Yeniden Adlandırma Modal'ı */}
            <Modal
                title="Klasörü Yeniden Adlandır"
                open={renameModalVisible}
                onCancel={() => setRenameModalVisible(false)}
                footer={null}
            >
                <Form
                    form={renameForm}
                    layout="vertical"
                    onFinish={(values) => handleRenameFolder(selectedFolder?.id || '', values.folderName)}
                >
                    <Form.Item
                        name="folderName"
                        label="Yeni Klasör Adı"
                        rules={[{ required: true, message: 'Lütfen yeni klasör adı girin' }]}
                    >
                        <Input placeholder="Yeni klasör adı girin" />
                    </Form.Item>
                    <Form.Item>
                        <Button type="primary" htmlType="submit" loading={loading}>
                            Yeniden Adlandır
                        </Button>
                    </Form.Item>
                </Form>
            </Modal>

            {/* Klasör Paylaşım Modal'ı */}
            <Modal
                title={`Klasör Paylaş: ${selectedFolderForShare?.name || ''}`}
                open={shareModalVisible}
                onCancel={() => setShareModalVisible(false)}
                footer={null}
            >
                <Form
                    form={shareForm}
                    layout="vertical"
                    onFinish={handleShareFolder}
                >
                    <Form.Item
                        name="email"
                        label="E-posta Adresi"
                        rules={[
                            { required: true, message: 'Lütfen e-posta adresi girin' },
                            { type: 'email', message: 'Geçerli bir e-posta adresi girin' }
                        ]}
                    >
                        <Input placeholder="Paylaşılacak kullanıcının e-posta adresi" />
                    </Form.Item>
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
                    <Form.Item>
                        <Button type="primary" htmlType="submit" loading={loading}>
                            Paylaş
                        </Button>
                    </Form.Item>
                </Form>
            </Modal>

            {/* Paylaşılan Kullanıcılar Modal'ı */}
            <Modal
                title={`Paylaşım Detayları: ${selectedFolderForSharedUsers?.name || ''}`}
                open={sharedUsersModalVisible}
                onCancel={() => setSharedUsersModalVisible(false)}
                footer={null}
                width={800}
            >
                {(() => {
                    // Kullanıcıları deduplike et (aynı kullanıcı birden fazla paylaşım kaydında olabilir)
                    const uniqueUsers = new Map();
                    sharedUsers.forEach(user => {
                        const userId = user.sharedWithUserId || user.userId || user.id;
                        if (userId && !uniqueUsers.has(userId)) {
                            uniqueUsers.set(userId, user);
                        }
                    });
                    const uniqueUsersArray = Array.from(uniqueUsers.values());
                    console.log('Deduplike edilmiş kullanıcı sayısı:', uniqueUsersArray.length, 'Orijinal sayı:', sharedUsers.length);
                    
                    return (
                        <Table
                            columns={[
                                { 
                                    title: 'Kullanıcı Adı', 
                                    dataIndex: 'sharedWithUserName', 
                                    key: 'username', 
                                    render: (text: string, record: any) => {
                                        // Veri modeli refaktörü sonrası farklı alanları kontrol et
                                        // Öncelik sırası: sharedWithUserName > name > username
                                        const userName = record.sharedWithUserName || 
                                                        record.name || 
                                                        record.username || 
                                                        'Bilinmeyen Kullanıcı';
                                        
                                        return <span>{userName}</span>;
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
                                        
                                        return <span>{email}</span>;
                                    }
                                },
                                { 
                                    title: 'Paylaşım Tarihi', 
                                    dataIndex: 'sharedAt', 
                                    key: 'sharedAt',
                                    render: (text: string) => text ? new Date(text).toLocaleString() : '-'
                                },
                                { 
                                    title: 'İzin Türü', 
                                    dataIndex: 'permissionType', 
                                    key: 'permissionType',
                                    render: (permissionType: number) => {
                                        const permissionMap: Record<number, string> = {
                                            [PermissionType.Read]: 'Okuma',
                                            [PermissionType.Write]: 'Yazma',
                                            [PermissionType.Delete]: 'Silme',
                                            [PermissionType.Share]: 'Paylaşım',
                                            [PermissionType.FullControl]: 'Tam Kontrol'
                                        };
                                        return permissionMap[permissionType] || 'Okuma';
                                    }
                                },
                                { 
                                    title: 'İşlemler', 
                                    key: 'action',
                                    render: (text: string, record: any) => {
                                        // Kullanıcı ID'si için tüm olası alanları kontrol et
                                        const userId = record.sharedWithUserId || record.userId || record.id;
                                        // Paylaşım kaydı ID'si
                                        const sharingRecordId = record.id;
                                        console.log('Tespit edilen kullanıcı ID:', userId, 'Paylaşım kaydı ID:', sharingRecordId);
                                        
                                        return (
                                            <Button 
                                                danger 
                                                onClick={() => handleRevokeAccess(sharingRecordId, userId)}
                                            >
                                                Erişimi İptal Et
                                            </Button>
                                        );
                                    }
                                }
                            ]}
                            dataSource={sharedUsers}
                            rowKey={(record: any) => {
                                // Her bir paylaşım kaydı için benzersiz bir key oluştur
                                // Burada kaydın kendi ID'sini (sharing record ID) kullanıyoruz, çünkü bu benzersiz olmalı
                                return record.id || Math.random().toString();
                            }}
                            loading={loadingSharedUsers}
                            pagination={false}
                        />
                    );
                })()}
            </Modal>
        </div>
    );
};

export default FolderManager;
