// "use memo";
import { Card, Skeleton, Alert } from 'antd';
import Paragraph from 'antd/es/typography/Paragraph';
import React, {
  useState,
  useEffect,
  useCallback,
  useMemo,
  useRef,
} from 'react';
import { useNavigate } from 'react-router-dom';

import { useCart } from '../contexts/CartContext';
import { Product } from '../models/Product';
import { ProductService } from '../services/productService';

import Button from './ui/Button';

interface ProductListProps {
  categoryId?: number;
  searchQuery?: string;
}

const ProductSkeleton: React.FC = () => {
  return (
    <Card
      className='product-card'
      aria-hidden='true'
      data-testid='product-skeleton'
    >
      <Skeleton.Image
        style={{ width: '100%', height: 200 }}
        active
      />
      <Skeleton
        active
        paragraph={{ rows: 4 }}
      />
    </Card>
  );
};

const ProductItem: React.FC<{
  product: Product;
  onAddToCart: (product: Product, e: React.MouseEvent) => void;
  addingToCart: number | null;
  onNavigate: (id: number) => void;
  onKeyDown: (
    id: number,
    e: React.KeyboardEvent<HTMLDivElement>
  ) => void;
}> = ({
  product,
  onAddToCart,
  addingToCart,
  onNavigate,
  onKeyDown,
}) => {
  const [imageLoaded, setImageLoaded] = useState(false);
  const [imageError, setImageError] = useState(false);
  const cardRef = useRef<HTMLDivElement>(null);
  const previousProductRef = useRef<{
    id: number;
    imageUrl: string | null;
  }>({ id: 0, imageUrl: null });

  const handleImageLoad = useCallback(() => {
    setImageLoaded(true);
  }, []);

  const handleImageError = useCallback(() => {
    setImageError(true);
  }, []);

  // Reset image state only when the product actually changes
  if (
    previousProductRef.current.id !== product.id ||
    previousProductRef.current.imageUrl !== product.imageUrl
  ) {
    // Product has changed, reset state
    previousProductRef.current = {
      id: product.id,
      imageUrl: product.imageUrl,
    };
    setImageLoaded(false);
    setImageError(false);
  }

  const imageUrl = product.imageUrl
    ? `${import.meta.env.VITE_BASE_URL || ''}${product.imageUrl}`
    : null;

  return (
    <Card
      ref={cardRef}
      key={product.id}
      className='product-card interactive'
      onClick={() => onNavigate(product.id)}
      onKeyDown={(e: React.KeyboardEvent<HTMLDivElement>) =>
        onKeyDown(product.id, e)
      }
      role='button'
      aria-label={`View details for product: ${product.name}`}
      tabIndex={0}
      cover={
        imageUrl && !imageError ? (
          <img
            src={imageUrl}
            alt={product.name}
            className={`product-image ${
              imageLoaded ? 'loaded' : 'loading'
            }`}
            onLoad={handleImageLoad}
            onError={handleImageError}
            loading='lazy'
          />
        ) : (
          <div
            className='product-image-placeholder'
            aria-label='Product image not available'
          >
            <span>No Image</span>
          </div>
        )
      }
      actions={[
        <Button
          key='add-to-cart'
          buttonStyle='primary'
          onClick={(e) => {
            e.stopPropagation();
            onAddToCart(product, e);
          }}
          disabled={addingToCart === product.id}
          ariaLabel={
            addingToCart === product.id
              ? `Adding ${product.name} to cart`
              : `Add ${product.name} to cart`
          }
          aria-busy={addingToCart === product.id}
          loading={addingToCart === product.id}
        >
          {addingToCart === product.id ? 'Adding...' : 'Add to Cart'}
        </Button>,
      ]}
    >
      <Card.Meta
        title={product.name}
        description={
          <>
            <p className='product-brand'>{product.brand}</p>
            <p className='product-price'>
              ${product.price.toFixed(2)}
            </p>
            <div className='product-attributes'>
              {product.size && (
                <span className='size'>Size: {product.size}</span>
              )}
              {product.color && (
                <span className='color'>Color: {product.color}</span>
              )}
            </div>
            <div className='product-verification'>
              {product.isVerified ? (
                <span
                  className='verified-badge'
                  aria-label='Verified product'
                >
                  ✓ Verified
                </span>
              ) : (
                <span
                  className='unverified-badge'
                  aria-label='Not verified product'
                >
                  ⚠ Not Verified
                </span>
              )}
              <span
                className='eco-score'
                aria-label={`Eco score: ${product.ecoScore} out of 100`}
              >
                EcoScore: {product.ecoScore}/100
              </span>
            </div>
          </>
        }
      />
    </Card>
  );
};

const ProductList: React.FC<ProductListProps> = ({
  categoryId,
  searchQuery,
}) => {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [addingToCart, setAddingToCart] = useState<number | null>(
    null
  );
  const navigate = useNavigate();
  const { addToCart } = useCart();
  const productListRef = useRef<HTMLDivElement>(null);

  const fetchProducts = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      let fetchedProducts: Product[] = [];

      if (searchQuery) {
        fetchedProducts = await ProductService.searchProducts(
          searchQuery
        );
      } else if (categoryId) {
        fetchedProducts = await ProductService.getProductsByCategory(
          categoryId
        );
      } else {
        fetchedProducts = await ProductService.getAllProducts();
      }

      setProducts(fetchedProducts);
    } catch (err) {
      setError('Failed to fetch products. Please try again later.');
      console.error('Error fetching products:', err);
    } finally {
      setLoading(false);
    }
  }, [categoryId, searchQuery]);

  useEffect(() => {
    void fetchProducts();
  }, [fetchProducts]);

  const handleAddToCart = useCallback(
    async (product: Product, e: React.MouseEvent) => {
      e.stopPropagation();
      try {
        setAddingToCart(product.id);
        await addToCart(product.id, 1);
      } catch (err) {
        console.error('Error adding to cart:', err);
        setError('Failed to add item to cart. Please try again.');
      } finally {
        setAddingToCart(null);
      }
    },
    [addToCart]
  );

  const handleNavigate = useCallback(
    (id: number) => {
      void navigate(`/product/${id}`);
    },
    [navigate]
  );

  const handleKeyDown = useCallback(
    (id: number, e: React.KeyboardEvent<HTMLDivElement>) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        handleNavigate(id);
      }
    },
    [handleNavigate]
  );

  // // Handle keyboard navigation for the entire product list
  // const handleListKeyDown = useCallback(
  //   (e: React.KeyboardEvent<HTMLDivElement>) => {
  //     if (e.key === 'ArrowRight' || e.key === 'ArrowLeft') {
  //       e.preventDefault();
  //       // Implement horizontal navigation between products if needed
  //     }
  //   },
  //   []
  // );

  // For large product lists, we can implement simple virtualization
  // by only rendering a subset of products at a time
  const VIRTUALIZATION_THRESHOLD = 50;
  const WINDOW_SIZE = 20;
  const [visibleStart, setVisibleStart] = useState(0);

  // Handle scroll for virtualization
  const handleScroll = useCallback(() => {
    if (
      productListRef.current &&
      products.length > VIRTUALIZATION_THRESHOLD
    ) {
      const scrollTop = productListRef.current.scrollTop;
      const cardHeight = 300; // Approximate height of a product card
      const newStart = Math.max(
        0,
        Math.floor(scrollTop / cardHeight) -
          Math.floor(WINDOW_SIZE / 2)
      );
      setVisibleStart(newStart);
    }
  }, [products.length]);

  // Attach scroll listener for virtualization
  useEffect(() => {
    if (
      productListRef.current &&
      products.length > VIRTUALIZATION_THRESHOLD
    ) {
      const container = productListRef.current;
      container.addEventListener('scroll', handleScroll);
      return () =>
        container.removeEventListener('scroll', handleScroll);
    }
  }, [handleScroll, products.length]);

  const productList = useMemo(() => {
    if (loading) {
      return (
        <div
          className='product-grid'
          aria-label='Loading products'
        >
          {Array.from({ length: 8 }).map((_, index) => (
            <ProductSkeleton key={index} />
          ))}
        </div>
      );
    }

    if (error) {
      return (
        <div
          className='product-list'
          role='alert'
          aria-live='assertive'
        >
          <Alert
            message='Error'
            description={error}
            type='error'
            showIcon
            action={
              <Button
                buttonStyle='primary'
                onClick={() => void fetchProducts()}
                ariaLabel='Retry fetching products'
              >
                Retry
              </Button>
            }
          />
        </div>
      );
    }

    if (products.length === 0) {
      return (
        <div className='product-list'>
          <Paragraph
            style={{ textAlign: 'center', marginBottom: '24px' }}
          >
            No products found.
          </Paragraph>
        </div>
      );
    }

    const shouldVirtualize =
      products.length > VIRTUALIZATION_THRESHOLD;
    const visibleProducts = shouldVirtualize
      ? products.slice(visibleStart, visibleStart + WINDOW_SIZE)
      : products;

    return (
      <div
        ref={productListRef}
        className='product-grid'
        aria-label='Product list'
        role="list"
        style={
          shouldVirtualize
            ? { height: '600px', overflowY: 'auto' }
            : {}
        }
      >
        {shouldVirtualize && (
          <div style={{ height: `${visibleStart * 300}px` }} />
        )}
        {visibleProducts.map((product) => (
          <ProductItem
            key={product.id}
            product={product}
            onAddToCart={(product, e) => void handleAddToCart(product, e)}
            addingToCart={addingToCart}
            onNavigate={handleNavigate}
            onKeyDown={handleKeyDown}
          />
        ))}
        {shouldVirtualize && (
          <div
            style={{
              height: `${
                (products.length - visibleStart - WINDOW_SIZE) * 300
              }px`,
            }}
          />
        )}
      </div>
    );
  }, [
    loading,
    error,
    products,
    handleAddToCart,
    addingToCart,
    handleNavigate,
    handleKeyDown,
    fetchProducts,
    visibleStart,
  ]);

  return (
    <div className='product-list'>
      {/* <Title level={2} style={{ textAlign: 'center', marginBottom: '24px' }}>Products</Title> */}
      {productList}
    </div>
  );
};

export default ProductList;
