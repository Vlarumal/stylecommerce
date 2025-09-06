import { Row, Col, Button } from 'antd';
import React from 'react';

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

interface CategoryFilterWrapperProps {
  categoryId: number | undefined;
  setCategoryId: (id: number | undefined) => void;
}

const CategoryFilterWrapper: React.FC<CategoryFilterWrapperProps> = ({ 
  categoryId, 
  setCategoryId 
}) => {
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

export default CategoryFilterWrapper;