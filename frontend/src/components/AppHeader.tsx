// "use memo";
import {
  Button,
  Typography,
  Layout,
  Row,
  Col,
  Space,
} from 'antd';
import React from 'react';
import { useNavigate } from 'react-router-dom';

import CartIcon from './CartIcon';

const { Header } = Layout;
const { Title, Paragraph } = Typography;

interface AppHeaderProps {
  isLoggedIn: boolean;
  theme: import('../types/theme').AppTheme;
  handleLogout: () => void;
}

const AppHeader: React.FC<AppHeaderProps> = ({
  isLoggedIn,
  theme,
  handleLogout,
}) => {
  const navigate = useNavigate();

  return (
    <Header
      style={{
        backgroundColor: theme.token?.colorPrimary,
        padding: '0 24px',
        height: 'auto',
        minHeight: '80px',
        boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
      }}
    >
      <Row
        align='middle'
        justify='space-between'
        style={{ height: '100%' }}
      >
        <Col
          xs={24}
          sm={12}
          md={12}
          lg={12}
          xl={12}
        >
          <Space
            direction='vertical'
            size='small'
          >
            <Title
              level={1}
              style={{
                color: 'white',
                margin: 0,
                fontSize: '2rem',
              }}
            >
              StyleCommerce
            </Title>
            <Paragraph
              style={{
                color: 'white',
                margin: 0,
                fontSize: '1.2rem',
              }}
            >
              Sustainable Fashion Marketplace
            </Paragraph>
          </Space>
        </Col>
        <Col
          xs={24}
          sm={12}
          md={12}
          lg={12}
          xl={12}
        >
          <div
            style={{
              display: 'flex',
              justifyContent: 'flex-end',
              alignItems: 'center',
              gap: '1rem',
              flexWrap: 'wrap',
            }}
          >
            {isLoggedIn ? (
              <>
                {/* <Button
                  href='/order-history'
                  type='default'
                  style={{
                    backgroundColor: 'white',
                    color: theme.token?.colorPrimary,
                    borderColor: 'white',
                  }}
                >
                  Order History
                </Button> */}
                {/* <Button
                  onClick={() => navigate('/admin/orders')}
                  type="default"
                  style={{ backgroundColor: 'white', color: theme.token.colorPrimary, borderColor: 'white' }}
                >
                  Admin
                </Button> */}
                <Button
                  onClick={handleLogout}
                  type='primary'
                  danger
                >
                  Logout
                </Button>
              </>
            ) : (
              <>
                <Button
                  onClick={() => {
                    void navigate('/login');
                  }}
                  type='default'
                  style={{
                    backgroundColor: 'white',
                    color: theme.token?.colorPrimary,
                    borderColor: 'white',
                  }}
                >
                  Login
                </Button>
                <Button
                  onClick={() => {
                    void navigate('/register');
                  }}
                  type='default'
                  style={{
                    backgroundColor: 'white',
                    color: theme.token?.colorPrimary,
                    borderColor: 'white',
                  }}
                >
                  Register
                </Button>
              </>
            )}
            <CartIcon />
          </div>
        </Col>
      </Row>
    </Header>
  );
};

export default AppHeader;