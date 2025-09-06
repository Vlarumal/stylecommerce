// "use memo";
import { DeleteOutlined, ShoppingCartOutlined } from '@ant-design/icons';
import { Table, InputNumber, Button, Empty, Spin, Alert, Popconfirm, Image, Typography, Card, Row, Col, Divider } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

import { useCart } from '../contexts/CartContext';

import Breadcrumbs from './Breadcrumbs';
// import './CartPage.css';

const { Text } = Typography;

interface CartItemData {
  key: number;
  productId: number;
  name: string;
  price: number;
  quantity: number;
  total: number;
  brand: string;
  ecoScore: number;
  imageUrl?: string;
  isProductValid: boolean;
}

const CartPage: React.FC = () => {
    const {
        cart,
        cartItemsCount,
        loading,
        error,
        fetchCart,
        updateCartItem,
        removeCartItem
    } = useCart();
    
    const navigate = useNavigate();

    useEffect(() => {
        void fetchCart();
    }, []);

    const handleQuantityChange = (productId: number, newQuantity: number) => {
        if (newQuantity < 1) {
            handleRemoveItem(productId);
            return;
        }
        void updateCartItem(productId, newQuantity);
    };

    const handleRemoveItem = (productId: number) => {
        void removeCartItem(productId);
    };

    const calculateSubtotal = () => {
        if (!cart) return 0;
        return cart.cartItems.reduce((total, item) => {
            if (!item.product) {
                console.warn('Product data missing for cart item - using priceSnapshot:', item);
                return total + (item.priceSnapshot * item.quantity);
            }
            return total + (item.product.price * item.quantity);
        }, 0);
    };

    const calculateTax = (subtotal: number) => {
        return subtotal * 0.08; // 8% tax
    };

    const calculateTotal = (subtotal: number, tax: number) => {
        return subtotal + tax;
    };

    if (loading) {
        return (
            <div className="cart-page">
                <Breadcrumbs />
                <div style={{ textAlign: 'center', padding: '50px 0' }}>
                    <Spin size="large" />
                    <p style={{ marginTop: 16 }}>Loading cart...</p>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="cart-page">
                <Breadcrumbs />
                <Alert
                    message="Error"
                    description={error}
                    type="error"
                    showIcon
                />
            </div>
        );
    }

    if (!cart || cartItemsCount === 0) {
        return (
            <div className="cart-page">
                <Breadcrumbs />
                <div style={{ textAlign: 'center', padding: '50px 0' }}>
                    <Empty
                        image={<ShoppingCartOutlined style={{ fontSize: 64, color: '#ccc' }} />}
                        description={
                            <span>
                                Your cart is empty
                            </span>
                        }
                    >
                        <Button type="primary" onClick={() => void navigate('/')}>
                            Continue Shopping
                        </Button>
                    </Empty>
                </div>
            </div>
        );
    }

    const subtotal = calculateSubtotal();
    const tax = calculateTax(subtotal);
    const total = calculateTotal(subtotal, tax);

    const cartItemsData: CartItemData[] = cart.cartItems.map(item => {
        const isProductValid = !!(item.product && item.product.name || item.priceSnapshot);
        const price = item.product?.price || item.priceSnapshot || 0;
        const name = item.product?.name || 'Product';
        const brand = item.product?.brand || 'Unknown Brand';
        const ecoScore = item.product?.ecoScore || 0;
        const imageUrl = item.product?.imageUrl;
        
        return {
            key: item.productId,
            productId: item.productId,
            name,
            price,
            quantity: item.quantity,
            total: price * item.quantity,
            brand,
            ecoScore,
            imageUrl,
            isProductValid
        };
    });

    const columns: ColumnsType<CartItemData> = [
        {
            title: 'Product',
            dataIndex: 'name',
            key: 'name',
            render: (_, record) => (
                <div style={{ display: 'flex', alignItems: 'center' }}>
                    {record.isProductValid ? (
                        <>
                            {record.imageUrl ? (
                                <Image
                                    width={60}
                                    src={`${import.meta.env.VITE_BASE_URL}${record.imageUrl}`}
                                    alt={record.name}
                                    style={{ borderRadius: 4, marginRight: 12 }}
                                    preview={false}
                                />
                            ) : (
                                <div style={{
                                    width: 60,
                                    height: 60,
                                    backgroundColor: '#f5f5f5',
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    marginRight: 12,
                                    borderRadius: 4
                                }}>
                                    <ShoppingCartOutlined style={{ fontSize: 20, color: '#999' }} />
                                </div>
                            )}
                            <div>
                                <Text strong>{record.name}</Text>
                                <br />
                                <Text type="secondary">{record.brand}</Text>
                                <br />
                                <Text>EcoScore: {record.ecoScore}/100</Text>
                            </div>
                        </>
                    ) : (
                        <div style={{ display: 'flex', alignItems: 'center' }}>
                            <div style={{
                                width: 60,
                                height: 60,
                                backgroundColor: '#f5f5f5',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                marginRight: 12,
                                borderRadius: 4
                            }}>
                                <span style={{ fontSize: 20, color: '#ff4d4f' }}>❌</span>
                            </div>
                            <div>
                                <Text strong type="danger">Product Unavailable</Text>
                                <br />
                                <Text type="secondary">This item is no longer available</Text>
                            </div>
                        </div>
                    )}
                </div>
            ),
        },
        {
            title: 'Price',
            dataIndex: 'price',
            key: 'price',
            render: (price: number) => `$${price.toFixed(2)}`,
            width: 100,
            responsive: ['md'],
        },
        {
            title: 'Quantity',
            dataIndex: 'quantity',
            key: 'quantity',
            render: (_, record) => (
                <InputNumber
                    min={1}
                    value={record.quantity}
                    onChange={(value) => handleQuantityChange(record.productId, value || 1)}
                    style={{ width: 60 }}
                />
            ),
            width: 120,
        },
        {
            title: 'Total',
            dataIndex: 'total',
            key: 'total',
            render: (total: number) => `$${total.toFixed(2)}`,
            width: 100,
            responsive: ['md'],
        },
        {
            title: 'Action',
            key: 'action',
            render: (_, record) => (
                <Popconfirm
                    title="Remove item"
                    description="Are you sure you want to remove this item from your cart?"
                    onConfirm={() => handleRemoveItem(record.productId)}
                    okText="Yes"
                    cancelText="No"
                >
                    <Button type="text" icon={<DeleteOutlined />} danger />
                </Popconfirm>
            ),
            width: 80,
        },
    ];

    return (
        <div className="cart-page">
            <Breadcrumbs />
            {/* <s>Your Cart</s> */}
            
            <Row gutter={[16, 16]}>
                <Col xs={24} lg={16}>
                    <Card>
                        <Table
                            dataSource={cartItemsData}
                            columns={columns}
                            pagination={false}
                            locale={{ emptyText: 'No items in cart' }}
                            scroll={{ x: true }}
                        />
                    </Card>
                </Col>
                
                <Col xs={24} lg={8}>
                    <Card title="Order Summary">
                        <div style={{ marginBottom: 16 }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8, flexWrap: 'wrap' }}>
                                <Text>Subtotal</Text>
                                <Text>${subtotal.toFixed(2)}</Text>
                            </div>
                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8, flexWrap: 'wrap' }}>
                                <Text>Tax</Text>
                                <Text>${tax.toFixed(2)}</Text>
                            </div>
                            <Divider style={{ margin: '8px 0' }} />
                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16, flexWrap: 'wrap' }}>
                                <Text strong>Total</Text>
                                <Text strong style={{ fontSize: '1.1em' }}>${total.toFixed(2)}</Text>
                            </div>
                        </div>
                        <Button
                            type="primary"
                            size="large"
                            block
                            onClick={() => void navigate('/checkout')}
                        >
                            Proceed to Checkout
                        </Button>
                    </Card>
                </Col>
            </Row>
        </div>
    );
};

export default CartPage;