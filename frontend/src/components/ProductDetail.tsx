// "use memo";
import {
  ShoppingCartOutlined,
  HeartOutlined,
  ArrowLeftOutlined,
  CameraOutlined
} from '@ant-design/icons';
import {
  Typography,
  Rate,
  Button,
  Select,
  Modal,
  Spin,
  Alert,
  Tag,
  Row,
  Col,
  Divider
} from 'antd';
import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

import { useCart } from '../contexts/CartContext';
import { Product } from '../models/Product';
import { AnalyticsService } from '../services/analyticsService';
import { ProductService } from '../services/productService';

import ARViewer from './ARViewer';
import Breadcrumbs from './Breadcrumbs';

const { Title, Text, Paragraph } = Typography;
const { Option } = Select;

const ProductDetail: React.FC = () => {
    const [product, setProduct] = useState<Product | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [showAR, setShowAR] = useState(false);
    const [badgeViewed, setBadgeViewed] = useState(false);
    const [quantity, setQuantity] = useState(1);
    const [addingToCart, setAddingToCart] = useState(false);
    const [addedToCart, setAddedToCart] = useState(false);
    
    const navigate = useNavigate();
    const { addToCart } = useCart();
    
    const { id } = useParams<{ id: string }>();
    const productId = id ? parseInt(id, 10) : null;
    
    // Track badge view once per product view
    useEffect(() => {
        if (product && !badgeViewed) {
            AnalyticsService.trackVerificationBadge(product.id, product.isVerified);
            setBadgeViewed(true);
        }
    }, [product, badgeViewed]);

    useEffect(() => {
        const fetchProduct = async () => {
            try {
                setLoading(true);
                if (productId === null) {
                    setError('Invalid product ID.');
                    return;
                }
                
                // In a real implementation, we would get the ID from route params or props
                const fetchedProduct = await ProductService.getProductById(productId);
                setProduct(fetchedProduct);
            } catch (err) {
                setError('Failed to fetch product details. Please try again.');
                console.error('Error fetching product:', err);
            } finally {
                setLoading(false);
            }
        };

        void fetchProduct();
    }, [productId]);

    const handleAddToCart = async () => {
        if (!product) return;
        
        try {
            setAddingToCart(true);
            await addToCart(product.id, quantity);
            setAddedToCart(true);
            setTimeout(() => setAddedToCart(false), 2000);
        } catch (err) {
            console.error('Error adding to cart:', err);
            setError('Failed to add item to cart. Please try again.');
        } finally {
            setAddingToCart(false);
        }
    };

    if (loading) {
        return (
            <div className="product-detail" style={{ textAlign: 'center', padding: '50px 0' }}>
                <Spin size="large" tip="Loading product details..." />
            </div>
        );
    }

    if (error) {
        return (
            <div className="product-detail" style={{ padding: '20px' }}>
                <Alert
                    message="Error"
                    description={error}
                    type="error"
                    showIcon
                />
            </div>
        );
    }

    if (!product) {
        return (
            <div className="product-detail" style={{ padding: '20px' }}>
                <Alert
                    message="Product Not Found"
                    description="The requested product could not be found."
                    type="warning"
                    showIcon
                />
            </div>
        );
    }

    return (
        <div className="product-detail" style={{ padding: '20px' }}>
            <Breadcrumbs productName={product.name} />
            
            <Row gutter={[24, 24]} style={{ marginTop: '20px' }}>
                {/* Product Images Column */}
                <Col xs={24} md={12}>
                    <div className="product-images">
                        {product.imageUrl && (
                            <img
                                src={`${import.meta.env.VITE_BASE_URL}${product.imageUrl}`}
                                alt={product.name}
                                style={{ width: '100%', height: 'auto', borderRadius: '8px' }}
                                onError={(e) => (e.currentTarget as HTMLImageElement).style.display = 'none'}
                            />
                        )}
                        {product.model3DUrl && (
                            <div style={{ marginTop: '20px' }}>
                                <Button
                                    type="primary"
                                    icon={<CameraOutlined />}
                                    onClick={() => {
                                        setShowAR(true);
                                        if (product) {
                                            AnalyticsService.trackAREngagement(product.id, product.name);
                                        }
                                    }}
                                    size="large"
                                    block
                                >
                                    Try On with AR
                                </Button>
                            </div>
                        )}
                    </div>
                </Col>
                
                {/* Product Info Column */}
                <Col xs={24} md={12}>
                    <div className="product-info">
                        <Title level={2} style={{ marginBottom: '8px' }}>{product.name}</Title>
                        
                        <Text type="secondary" style={{ fontSize: '16px', display: 'block', marginBottom: '16px' }}>
                            {product.brand}
                        </Text>
                        
                        <Title level={3} style={{ color: '#fa8c16', marginBottom: '16px' }}>
                            ${product.price.toFixed(2)}
                        </Title>
                        
                        <div style={{ marginBottom: '16px' }}>
                            <Text strong>Eco Rating: </Text>
                            <Rate
                                disabled
                                value={product.ecoScore / 20}
                                tooltips={[`${product.ecoScore}/100`]}
                                style={{ color: '#52c41a' }}
                                allowHalf
                            />
                        </div>
                        
                        <div className="product-attributes" style={{ marginBottom: '16px' }}>
                            {product.size && (
                                <Tag color="blue">Size: {product.size}</Tag>
                            )}
                            {product.color && (
                                <Tag color="purple">Color: {product.color}</Tag>
                            )}
                        </div>
                        
                        <div className="product-verification" style={{ marginBottom: '24px' }}>
                            {product.isVerified ? (
                                <Tag color="green" icon={<span>✓</span>}>
                                    Verified Sustainable
                                </Tag>
                            ) : (
                                <Tag color="orange" icon={<span>⚠</span>}>
                                    Not Verified
                                </Tag>
                            )}
                            <Text style={{ marginLeft: '10px' }}>
                                EcoScore: <Text strong>{product.ecoScore}/100</Text>
                            </Text>
                        </div>
                        
                        <Divider />
                        
                        <div className="product-description" style={{ marginBottom: '24px' }}>
                            <Title level={4}>Description</Title>
                            <Paragraph>
                                {product.description}
                            </Paragraph>
                        </div>
                        
                        <Divider />
                        
                        <div className="product-actions">
                            <div style={{ marginBottom: '16px' }}>
                                <Text strong style={{ marginRight: '10px' }}>Quantity:</Text>
                                <Select
                                    value={quantity}
                                    onChange={(value) => setQuantity(value)}
                                    style={{ width: '80px' }}
                                >
                                    {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map(num => (
                                        <Option key={num} value={num}>{num}</Option>
                                    ))}
                                </Select>
                            </div>
                            
                            <div style={{ marginBottom: '16px' }}>
                                <Button
                                    type="primary"
                                    icon={<ShoppingCartOutlined />}
                                    onClick={() => void handleAddToCart()}
                                    loading={addingToCart}
                                    size="large"
                                    style={{ marginRight: '10px', marginBottom: '10px' }}
                                >
                                    {addingToCart ? 'Adding...' : addedToCart ? 'Added to Cart!' : 'Add to Cart'}
                                </Button>
                                
                                <Button
                                    icon={<HeartOutlined />}
                                    size="large"
                                    style={{ marginRight: '10px', marginBottom: '10px' }}
                                >
                                    Add to Wishlist
                                </Button>
                                
                                <Button
                                    icon={<ArrowLeftOutlined />}
                                    onClick={() => void navigate(-1)}
                                    size="large"
                                    style={{ marginBottom: '10px' }}
                                >
                                    Back to Products
                                </Button>
                            </div>
                        </div>
                    </div>
                </Col>
            </Row>
            
            <Modal
                open={showAR}
                onCancel={() => setShowAR(false)}
                footer={null}
                width="80%"
                style={{ top: 20 }}
            >
                <ARViewer
                    productId={product.id}
                    productName={product.name}
                    onClose={() => setShowAR(false)}
                />
            </Modal>
        </div>
    );
};

export default ProductDetail;