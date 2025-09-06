import { Card, Button } from 'antd';
import React from 'react';
import { useNavigate } from 'react-router-dom';

import { categories } from '../constants/categories';

const CategoryFilter: React.FC = () => {
  const navigate = useNavigate();

  const handleCategoryClick = (categoryId: string) => {
    void navigate(`/category/${categoryId}`);
  };

  return (
    <div style={{ padding: '20px 0' }}>
      <h2 style={{ textAlign: 'center', marginBottom: '20px' }}>Shop by Category</h2>
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          flexWrap: 'wrap',
          gap: '20px',
        }}
      >
        {categories.map((category) => (
          <Card
            key={category.id}
            hoverable
            style={{ width: 200 }}
            cover={
              <img
                alt={category.name}
                src={category.image}
                style={{ height: 150, objectFit: 'cover' }}
              />
            }
            onClick={() => handleCategoryClick(category.id)}
          >
            <Card.Meta
              title={
                <Button type='link' style={{ padding: 0, height: 'auto' }}>
                  {category.name}
                </Button>
              }
            />
          </Card>
        ))}
      </div>
    </div>
  );
};

export default CategoryFilter;