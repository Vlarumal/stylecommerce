// "use memo";
import {
  Button,
  Input,
  Form,
  Layout,
  Row,
  Col,
  Divider,
  Space,
} from 'antd';
import React, { useState } from 'react';
import { Routes, Route, Navigate, useLocation } from 'react-router-dom';

import { AppTheme } from '../types/theme';

import OrderManagementPage from './admin/OrderManagementPage';
import AuthRedirectWrapper from './AuthRedirectWrapper';
import CartPage from './CartPage';
import CheckoutPage from './CheckoutPage';
import LoginPageContent from './LoginPageContent';
import type { RedirectState } from './LoginPageContent';
import OrderConfirmationPage from './OrderConfirmationPage';
import OrderDetailsPage from './OrderDetailsPage';
import OrderHistoryPage from './OrderHistoryPage';
import ProductDetail from './ProductDetail';
import ProductList from './ProductList';
import RegistrationPageContent from './RegistrationPageContent';

const { Content } = Layout;

interface AppContentProps {
  isLoggedIn: boolean;
  theme: AppTheme;
  handleLogin: (values: { username: string; password: string }, locationState?: RedirectState) => void;
}

interface Category {
  id: number | undefined;
  name: string;
}

const categories: Category[] = [
  { id: undefined, name: 'All Products' },
  { id: 1, name: 'Tops' },
  { id: 2, name: 'Bottoms' },
  { id: 3, name: 'Accessories' },
];


// Separate component for search form (AppContent-specific implementation)
// Note: This is a simplified version compared to the standalone SearchForm component
// The standalone version has navigation built-in and uses its own state management
const SearchForm: React.FC<{
  searchQuery: string;
  setSearchQuery: (value: string) => void;
  handleSearch: () => void;
}> = ({ searchQuery, setSearchQuery, handleSearch }) => {
  return (
    <Form onFinish={handleSearch}>
      <Row
        justify='center'
        gutter={16}
      >
        <Col
          xs={24}
          sm={16}
          md={12}
          lg={8}
        >
          <Form.Item>
            <Input
              type='text'
              placeholder='Search products...'
              value={searchQuery}
              onChange={(e) =>
                setSearchQuery(e.target.value)
              }
              size='large'
            />
          </Form.Item>
        </Col>
        <Col
          xs={24}
          sm={8}
          md={6}
          lg={4}
        >
          <Form.Item>
            <Button
              type='primary'
              htmlType='submit'
              size='large'
              style={{ width: '100%' }}
            >
              Search
            </Button>
          </Form.Item>
        </Col>
      </Row>
    </Form>
  );
};

// Separate component for category filters (AppContent-specific implementation)
// Note: This is a simplified version compared to the standalone CategoryFilter component
// The standalone version uses Card components with images and has navigation built-in
const CategoryFilter: React.FC<{
  categoryId: number | undefined;
  setCategoryId: (id: number | undefined) => void;
}> = ({ categoryId, setCategoryId }) => {
  return (
    <Row
      justify='center'
      gutter={[16, 16]}
    >
      {categories.map((category) => (
        <Col key={category.id?.toString() || 'all'}>
          <Button
            onClick={() => setCategoryId(category.id)}
            type={
              categoryId === category.id
                ? 'primary'
                : 'default'
            }
          >
            {category.name}
          </Button>
        </Col>
      ))}
    </Row>
  );
};

// Separate component for home page content (AppContent-specific implementation)
// Note: This is a simplified version compared to the standalone HomePageContent component
// The standalone version uses its own state and composes standalone SearchForm and CategoryFilter components
const HomePageContent: React.FC<{
  searchQuery: string;
  setSearchQuery: (value: string) => void;
  categoryId: number | undefined;
  setCategoryId: (id: number | undefined) => void;
  handleSearch: () => void;
}> = ({ searchQuery, setSearchQuery, categoryId, setCategoryId, handleSearch }) => {
  return (
    <Space
      direction='vertical'
      size='large'
      style={{ width: '100%' }}
    >
      <div>
        <SearchForm
          searchQuery={searchQuery}
          setSearchQuery={setSearchQuery}
          handleSearch={handleSearch}
        />
        <Divider style={{ margin: '24px 0' }} />
      </div>

      <div>
        <CategoryFilter
          categoryId={categoryId}
          setCategoryId={setCategoryId}
        />
      </div>

      <div>
        <ProductList
          categoryId={categoryId}
          searchQuery={searchQuery}
        />
      </div>
    </Space>
  );
};

// Wrapper component for LoginPageContent to access location state
const LoginPageWrapper: React.FC<{
  theme: AppTheme;
  handleLogin: (values: { username: string; password: string }, locationState?: RedirectState) => void;
}> = ({ theme, handleLogin }) => {
  const location = useLocation();
  
  return (
    <LoginPageContent
      theme={theme}
      handleLogin={(values, locationState) => handleLogin(values, locationState || location.state as RedirectState)}
    />
  );
};

const AppContent: React.FC<AppContentProps> = ({
  isLoggedIn,
  theme,
  handleLogin,
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [categoryId, setCategoryId] = useState<number | undefined>(
    undefined
  );

  const handleSearch = () => {
    // Search is handled by ProductList component through props
    // This function exists to satisfy the form's onFinish requirement
  };

  return (
    <Content
      style={{
        padding: '24px',
        minHeight: 'calc(100vh - 140px)',
      }}
    >
      <Routes>
        <Route
          path='/'
          element={
            <HomePageContent
              searchQuery={searchQuery}
              setSearchQuery={setSearchQuery}
              categoryId={categoryId}
              setCategoryId={setCategoryId}
              handleSearch={handleSearch}
            />
          }
          handle={{
            crumb: () => 'Home',
          }}
        />
        <Route
          path='/product/:id'
          element={<ProductDetail />}
          handle={{
            crumb: (data: { product?: { name: string } }) => data?.product?.name || 'Product',
          }}
        />
        <Route
          path='/cart'
          element={<CartPage />}
          handle={{
            crumb: () => 'Cart',
          }}
        />
        <Route
          path='/checkout'
          element={
            <AuthRedirectWrapper
              isLoggedIn={isLoggedIn}
              element={<CheckoutPage />}
            />
          }
          handle={{
            crumb: () => 'Checkout',
          }}
        />
        <Route
          path='/order-confirmation/:id'
          element={
            <AuthRedirectWrapper
              isLoggedIn={isLoggedIn}
              element={<OrderConfirmationPage />}
            />
          }
          handle={{
            crumb: () => 'Order Confirmation',
          }}
        />
        <Route
          path='/order-history'
          element={
            <AuthRedirectWrapper
              isLoggedIn={isLoggedIn}
              element={<OrderHistoryPage />}
            />
          }
          handle={{
            crumb: () => 'Order History',
          }}
        />
        <Route
          path='/order-details/:id'
          element={
            <AuthRedirectWrapper
              isLoggedIn={isLoggedIn}
              element={<OrderDetailsPage />}
            />
          }
          handle={{
            crumb: () => 'Order Details',
          }}
        />
        <Route
          path='/admin/orders'
          element={
            <AuthRedirectWrapper
              isLoggedIn={isLoggedIn}
              element={<OrderManagementPage />}
            />
          }
          handle={{
            crumb: () => 'Admin Orders',
          }}
        />
        <Route
          path='/login'
          element={
            isLoggedIn ? (
              <Navigate
                to='/'
                replace
              />
            ) : (
              <div
                style={{
                  maxWidth: '400px',
                  margin: '2rem auto',
                }}
              >
                <LoginPageWrapper
                  theme={theme}
                  handleLogin={handleLogin}
                />
              </div>
            )
          }
          handle={{
            crumb: () => 'Login',
          }}
        />
        <Route
          path='/register'
          element={
            isLoggedIn ? (
              <Navigate
                to='/'
                replace
              />
            ) : (
              <div
                style={{
                  maxWidth: '400px',
                  margin: '2rem auto',
                }}
              >
                <RegistrationPageContent />
              </div>
            )
          }
          handle={{
            crumb: () => 'Register',
          }}
        />
      </Routes>
    </Content>
  );
};

export default AppContent;
