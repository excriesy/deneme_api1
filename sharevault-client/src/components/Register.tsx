import React from 'react';
import { Form, Input, Button, Card, message } from 'antd';
import { UserOutlined, LockOutlined, MailOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const Register: React.FC = () => {
    const navigate = useNavigate();
    const { register } = useAuth();
    const [form] = Form.useForm();

    const onFinish = async (values: { username: string; email: string; password: string }) => {
        try {
            await register(values.username, values.email, values.password);
            message.success('Kayıt başarılı');
            navigate('/login');
        } catch (error) {
            message.error('Kayıt başarısız');
        }
    };

    return (
        <div style={{ maxWidth: 400, margin: '100px auto' }}>
            <Card title="Kayıt Ol">
                <Form
                    form={form}
                    name="register"
                    onFinish={onFinish}
                    layout="vertical"
                >
                    <Form.Item
                        name="username"
                        rules={[{ required: true, message: 'Lütfen kullanıcı adınızı girin' }]}
                    >
                        <Input prefix={<UserOutlined />} placeholder="Kullanıcı Adı" />
                    </Form.Item>

                    <Form.Item
                        name="email"
                        rules={[
                            { required: true, message: 'Lütfen e-posta adresinizi girin' },
                            { type: 'email', message: 'Geçerli bir e-posta adresi girin' }
                        ]}
                    >
                        <Input prefix={<MailOutlined />} placeholder="E-posta" />
                    </Form.Item>

                    <Form.Item
                        name="password"
                        rules={[
                            { required: true, message: 'Lütfen şifrenizi girin' },
                            { min: 6, message: 'Şifre en az 6 karakter olmalıdır' }
                        ]}
                    >
                        <Input.Password prefix={<LockOutlined />} placeholder="Şifre" />
                    </Form.Item>

                    <Form.Item>
                        <Button type="primary" htmlType="submit" block>
                            Kayıt Ol
                        </Button>
                    </Form.Item>
                </Form>
            </Card>
        </div>
    );
};

export default Register; 