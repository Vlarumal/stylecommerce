import { SearchOutlined } from '@ant-design/icons';
import { Form, Input, Button } from 'antd';
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

const SearchForm: React.FC = () => {
  const [searchQuery, setSearchQuery] = useState('');
  const navigate = useNavigate();

  const handleSearch = (values: { query: string }) => {
    void navigate(`/search?q=${encodeURIComponent(values.query)}`);
  };

  return (
    <Form layout='inline' onFinish={handleSearch}>
      <Form.Item
        name='query'
        initialValue={searchQuery}
        style={{ flex: 1, minWidth: 200 }}
      >
        <Input
          placeholder='Search products...'
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          suffix={<SearchOutlined />}
        />
      </Form.Item>
      <Form.Item>
        <Button type='primary' htmlType='submit'>
          Search
        </Button>
      </Form.Item>
    </Form>
  );
};

export default SearchForm;