import { Form, Input, Button, Row, Col } from 'antd';
import React from 'react';

interface SearchFormWrapperProps {
  searchQuery: string;
  setSearchQuery: (value: string) => void;
  handleSearch: () => void;
}

const SearchFormWrapper: React.FC<SearchFormWrapperProps> = ({ 
  searchQuery, 
  setSearchQuery, 
  handleSearch 
}) => {
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

export default SearchFormWrapper;