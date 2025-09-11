import { ConfigProvider, ThemeConfig, message } from 'antd';
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

import AppContent from './components/AppContent';
import AppFooter from './components/AppFooter';
import AppHeader from './components/AppHeader';
import { CartProvider } from './contexts/CartContext';
import { AnalyticsService } from './services/analyticsService';
import {
  AuthService,
  LoginRequest,
} from './services/authService';
import './App.css';

interface RedirectState {
  from?: {
    pathname: string;
    search?: string;
  }
}

// Ant Design theme configuration
const theme: ThemeConfig = {
  token: {
    colorPrimary: '#28a745',
    colorSuccess: '#28a745',
    colorWarning: '#ffc107',
    colorError: '#dc3545',
    colorInfo: '#17a2b8',
    borderRadius: 6,
    fontFamily: `-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial,
      'Noto Sans', sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol',
      'Noto Color Emoji'`,
  },
  components: {
    Button: {
      borderRadius: 6,
      controlHeight: 40,
    },
    Input: {
      borderRadius: 6,
      controlHeight: 40,
    },
  },
};

const App: React.FC = () => {
  const navigate = useNavigate();
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  useEffect(() => {
    const checkAuthStatus = async () => {
      try {
        const authResponse = await AuthService.checkAuth();
        if (authResponse.isAuthenticated) {
          setIsLoggedIn(true);
        } else {
          setIsLoggedIn(false);
        }
      } catch (error) {
        console.error(
          'Failed to check authentication status:',
          error
        );
        setIsLoggedIn(false);
      }
    };

    void checkAuthStatus();
    AnalyticsService.trackEvent('Application Started');
  }, []);

  const handleLogin = async (values: LoginRequest, locationState?: RedirectState) => {
    try {
      const credentials: LoginRequest = {
        username: values.username,
        password: values.password,
      };

      await AuthService.login(credentials);


      setIsLoggedIn(true);

      const navigateWrapper = (path: string) => {
        void navigate(path);
      };
      
      AuthService.handlePostLoginRedirect(navigateWrapper, locationState);
    } catch (err) {
      console.error('Login error:', err);
      const errorMessage =
        err instanceof Error
          ? err.message
          : 'Login failed. Please check your credentials.';
      message.error(errorMessage);
    }
  };

  const handleLogout = () => {
    void AuthService.logout();
    setIsLoggedIn(false);
    message.success('You have been logged out successfully');
    void navigate('/');
  };

  return (
    <ConfigProvider theme={theme}>
      <CartProvider>
        <div
          style={{
            minHeight: '100vh',
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          <AppHeader
            isLoggedIn={isLoggedIn}
            theme={theme}
            handleLogout={handleLogout}
          />
          <div style={{ flex: 1 }}>
            <AppContent
              isLoggedIn={isLoggedIn}
              theme={theme}
              handleLogin={(values, locationState) => {
                void handleLogin(values, locationState);
              }}
            />
          </div>
          <AppFooter />
        </div>
      </CartProvider>
    </ConfigProvider>
  );
};

export default App;
