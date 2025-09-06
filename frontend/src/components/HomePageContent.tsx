import { Layout, Typography } from 'antd';
import React, { useState } from 'react';

import CategoryFilter from './CategoryFilter';
import CategoryFilterWrapper from './CategoryFilterWrapper';
import ProductList from './ProductList';
import SearchForm from './SearchForm';
import SearchFormWrapper from './SearchFormWrapper';


const { Content } = Layout;
const { Title } = Typography;

interface HomePageContentProps {
  searchQuery?: string;
  setSearchQuery?: (query: string) => void;
  categoryId?: number | undefined;
  setCategoryId?: (id: number | undefined) => void;
  handleSearch?: () => void;
}

const HomePageContent: React.FC<HomePageContentProps> = ({
  searchQuery,
  setSearchQuery,
  categoryId,
  setCategoryId,
  handleSearch
}) => {
  const [internalSearchQuery] = useState('');
  const [internalCategoryId] = useState<number | undefined>(undefined);
  
  const effectiveSearchQuery = searchQuery !== undefined ? searchQuery : internalSearchQuery;
  const effectiveCategoryId = categoryId !== undefined ? categoryId : internalCategoryId;

  return (
    <Content style={{ padding: '0 24px', minHeight: 'calc(100vh - 120px)' }}>
      <div style={{ padding: '24px 0' }}>
        <Title style={{ textAlign: 'center', marginBottom: '20px' }}>
          Welcome to StyleCommerce
        </Title>
        <div style={{ maxWidth: '600px', margin: '0 auto 30px' }}>
          {searchQuery !== undefined && setSearchQuery !== undefined && handleSearch !== undefined ? (
            <SearchFormWrapper
              searchQuery={searchQuery}
              setSearchQuery={setSearchQuery}
              handleSearch={handleSearch}
            />
          ) : (
            <SearchForm />
          )}
        </div>
        {categoryId !== undefined && setCategoryId !== undefined ? (
          <CategoryFilterWrapper
            categoryId={categoryId}
            setCategoryId={setCategoryId}
          />
        ) : (
          <CategoryFilter />
        )}
        <div style={{ marginTop: '40px' }}>
          <Title level={2} style={{ textAlign: 'center' }}>
            Featured Products
          </Title>
          <ProductList
            categoryId={effectiveCategoryId}
            searchQuery={effectiveSearchQuery}
          />
        </div>
      </div>
    </Content>
  );
};

export default HomePageContent;