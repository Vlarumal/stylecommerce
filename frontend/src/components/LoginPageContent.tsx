import { UserOutlined, LockOutlined } from '@ant-design/icons';
import { Form, Input, Button, Card, Typography, message } from 'antd';
import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';

import { AuthService } from '../services/authService';

const { Title } = Typography;

interface LoginPageContentProps {
  setIsLoggedIn?: (isLoggedIn: boolean) => void;
  handleLogin?: (values: {
    username: string;
    password: string;
  }) => void;
}

interface LocationState {
  from?: {
    pathname?: string;
  };
}

const LoginPageContent: React.FC<LoginPageContentProps> = ({
  setIsLoggedIn,
  handleLogin,
}) => {
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  const handleSubmit = async (values: {
    username: string;
    password: string;
  }) => {
    setLoading(true);
    try {
      // If handleLogin is provided, use it (for AppContent integration)
      if (handleLogin) {
        handleLogin(values);
        return;
      }

      // Otherwise, use the default AuthService login
      const result = await AuthService.login({
        username: values.username,
        password: values.password,
      });
      if (result.success && setIsLoggedIn) {
        setIsLoggedIn(true);
        message.success('Login successful');

        // Let AuthService handle the redirect
        AuthService.handlePostLoginRedirect((path: string) => {
          void navigate(path);
        }, location.state as LocationState);
      } else {
        message.error('Login failed');
      }
    } catch {
      message.error('An error occurred during login');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '80vh',
      }}
    >
      <Card style={{ width: 400 }}>
        <Title
          level={2}
          style={{ textAlign: 'center' }}
        >
          Login
        </Title>
        <Form
          name='login'
          onFinish={void handleSubmit}
          autoComplete='off'
        >
          <Form.Item
            name='username'
            rules={[
              {
                required: true,
                message: 'Please input your username!',
              },
            ]}
          >
            <Input
              prefix={<UserOutlined />}
              placeholder='Username'
            />
          </Form.Item>
          <Form.Item
            name='password'
            rules={[
              {
                required: true,
                message: 'Please input your password!',
              },
            ]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder='Password'
            />
          </Form.Item>
          <Form.Item>
            <Button
              type='primary'
              htmlType='submit'
              loading={loading}
              block
            >
              Log in
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default LoginPageContent;
