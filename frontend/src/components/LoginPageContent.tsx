import { UserOutlined, LockOutlined } from '@ant-design/icons';
import { Form, Input, Button, Card, Typography, message } from 'antd';
import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';

import { AuthService } from '../services/authService';
import { AppTheme } from '../types/theme';

import Breadcrumbs from './Breadcrumbs';

const { Title } = Typography;

export type RedirectState = {
  from?: {
    pathname: string;
    search?: string;
  }
};

interface LoginPageContentProps {
  setIsLoggedIn?: (isLoggedIn: boolean) => void;
  handleLogin?: (values: { username: string; password: string }, locationState?: RedirectState) => void;
  theme?: AppTheme;
}

const LoginPageContent: React.FC<LoginPageContentProps> = ({
  setIsLoggedIn,
  handleLogin,
  theme,
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();
  const location = useLocation();
  
  const redirectPath = (location.state as RedirectState)?.from?.pathname || '/';

  const handleSubmit = (values: { username: string; password: string }) => {
    const executeLogin = async () => {
      setLoading(true);
      setError(null);
      
      try {
        if (handleLogin) {
          handleLogin(values, location.state as RedirectState);
          return;
        }

        const result = await AuthService.login(values);
        if (result.success && setIsLoggedIn) {
          setIsLoggedIn(true);
          message.success('Login successful');
          
          void navigate(redirectPath, { replace: true });
        } else {
          setError('Login failed');
        }
      } catch {
        setError('An error occurred during login');
      } finally {
        setLoading(false);
      }
    };

    void executeLogin();
  };

  const ErrorMessage = ({ message }: { message: string }) => (
    <div style={{
      color: theme?.token?.colorError || '#dc3545',
      backgroundColor: '#f8d7da',
      border: '1px solid #f5c6cb',
      padding: '0.75rem',
      borderRadius: '4px',
      marginBottom: '1rem',
    }}>
      {message}
    </div>
  );

  return (
    <div style={{
      display: 'flex',
      justifyContent: 'center',
      alignItems: 'center',
      minHeight: '80vh'
    }}>
      <Card style={{ width: 400 }}>
        <Breadcrumbs />
        <Title level={2} style={{ textAlign: 'center' }}>Login</Title>
        
        {error && <ErrorMessage message={error} />}
        
        <Form name="login" onFinish={handleSubmit} autoComplete="off">
          <Form.Item
            name="username"
            rules={[{ required: true, message: 'Please input your username!' }]}
          >
            <Input prefix={<UserOutlined />} placeholder="Username" autoComplete="username" />
          </Form.Item>
          
          <Form.Item
            name="password"
            rules={[{ required: true, message: 'Please input your password!' }]}
          >
            <Input.Password prefix={<LockOutlined />} placeholder="Password" autoComplete="current-password" />
          </Form.Item>
          
          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              loading={loading}
              block
            >
              Log in
            </Button>
          </Form.Item>
          
          <Form.Item>
            <Button
              type="link"
              onClick={() => void navigate('/register')}
              block
            >
              Don't have an account? Register
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default LoginPageContent;
