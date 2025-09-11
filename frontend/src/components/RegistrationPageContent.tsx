import {
  UserOutlined,
  LockOutlined,
  MailOutlined,
} from '@ant-design/icons';
import { Form, Input, Button, Card, Typography, message } from 'antd';
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { AuthService } from '../services/authService';

import Breadcrumbs from './Breadcrumbs';

const { Title } = Typography;

interface RegistrationPageContentProps {
  setIsLoggedIn?: (isLoggedIn: boolean) => void;
}

const RegistrationPageContent: React.FC<
  RegistrationPageContentProps
> = ({ setIsLoggedIn: _setIsLoggedIn }) => {
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = (values: {
    username: string;
    password: string;
    email: string;
    firstName?: string;
    lastName?: string;
    confirmPassword: string;
  }) => {
    setLoading(true);
    AuthService.register({
      username: values.username,
      password: values.password,
      email: values.email,
      firstName: values.firstName,
      lastName: values.lastName,
    })
      .then((result) => {
        if (result.success) {
          message.success('Registration successful');
          // Redirect to login page
          void navigate('/login');
        } else {
          message.error(result.message || 'Registration failed');
        }
      })
      .catch((error: unknown) => {
        if (typeof error === 'object' && error !== null && 'message' in error) {
          message.error((error as { message: string }).message || 'An error occurred during registration');
        } else {
          message.error('An error occurred during registration');
        }
      })
      .finally(() => {
        setLoading(false);
      });
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
        <Breadcrumbs />
        <Title
          level={2}
          style={{ textAlign: 'center' }}
        >
          Register
        </Title>
        <Form
          name='register'
          onFinish={handleSubmit}
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
            name='email'
            rules={[
              {
                required: true,
                message: 'Please input your email!',
              },
              {
                type: 'email',
                message: 'Please enter a valid email!',
              },
            ]}
          >
            <Input
              prefix={<MailOutlined />}
              placeholder='Email'
            />
          </Form.Item>
          
          <Form.Item name='firstName'>
            <Input
              prefix={<UserOutlined />}
              placeholder='First Name (Optional)'
            />
          </Form.Item>
          
          <Form.Item name='lastName'>
            <Input
              prefix={<UserOutlined />}
              placeholder='Last Name (Optional)'
            />
          </Form.Item>
          
          <Form.Item
            name='password'
            rules={[
              {
                required: true,
                message: 'Please input your password!',
              },
              {
                min: 6,
                message: 'Password must be at least 8 characters!',
              },
            ]}
            hasFeedback
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder='Password'
            />
          </Form.Item>
          
          <Form.Item
            name='confirmPassword'
            dependencies={['password']}
            rules={[
              {
                required: true,
                message: 'Please confirm your password!',
              },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('password') === value) {
                    return Promise.resolve();
                  }
                  return Promise.reject(
                    new Error(
                      'The two passwords that you entered do not match!'
                    )
                  );
                },
              }),
            ]}
            hasFeedback
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder='Confirm Password'
            />
          </Form.Item>
          
          <Form.Item>
            <Button
              type='primary'
              htmlType='submit'
              loading={loading}
              block
            >
              Register
            </Button>
          </Form.Item>
          
          <Form.Item>
            <Button
              type='link'
              onClick={() => void navigate('/login')}
              block
            >
              Already have an account? Login
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default RegistrationPageContent;
