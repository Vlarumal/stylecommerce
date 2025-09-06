import { Layout, Typography } from 'antd';
import React from 'react';

const { Footer } = Layout;
const { Paragraph } = Typography;


const AppFooter: React.FC = () => {
  return (
    <Footer style={{ textAlign: 'center', padding: '24px 0' }}>
      <Paragraph style={{ margin: 0 }}>
        © 2025 StyleCommerce. Sustainable Fashion Marketplace.
      </Paragraph>
    </Footer>
  );
};

export default AppFooter;