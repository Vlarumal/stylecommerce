import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

import { OrderService } from '../services/orderService';

import Breadcrumbs from './Breadcrumbs';

interface OrderItem {
  orderItemId: number;
  orderId: number;
  productId: number;
  quantity: number;
  price: number;
  product: {
    id: number;
    name: string;
    description: string;
    price: number;
    categoryId: number;
    brand: string;
    size: string;
    color: string;
    stockQuantity: number;
    imageUrl: string;
    model3DUrl?: string;
    createdAt: string;
    updatedAt: string;
    isVerified: boolean;
    verificationScore: number;
    ecoScore: number;
  };
}

interface Order {
  orderId: number;
  userId: number;
  orderDate: string;
  totalAmount: number;
  status: string;
  orderItems: OrderItem[];
}

const OrderConfirmationPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [order, setOrder] = useState<Order | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchOrderDetails = async () => {
      if (!id) {
        setError('Order ID is missing');
        setLoading(false);
        return;
      }

      try {
        const orderId = parseInt(id, 10);
        if (isNaN(orderId)) {
          setError('Invalid order ID');
          setLoading(false);
          return;
        }

        const orderData = await OrderService.getOrderDetails(orderId);
        setOrder(orderData);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to fetch order details');
        console.error('Error fetching order details:', err);
      } finally {
        setLoading(false);
      }
    };

    void fetchOrderDetails();
  }, [id]);

  if (loading) {
    return (
      <div className="order-confirmation-page">
        <div className="container">
          <Breadcrumbs />
          <h1>Order Confirmation</h1>
          <p>Loading order details...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="order-confirmation-page">
        <div className="container">
          <Breadcrumbs />
          <h1>Order Confirmation</h1>
          <div className="error-message">
            <p>{error}</p>
            <button onClick={() => void navigate('/')} className="btn-primary">
              Continue Shopping
            </button>
          </div>
        </div>
      </div>
    );
  }

  if (!order) {
    return (
      <div className="order-confirmation-page">
        <div className="container">
          <Breadcrumbs />
          <h1>Order Confirmation</h1>
          <p>Order not found.</p>
        </div>
      </div>
    );
  }

  // Calculate estimated delivery date (3 business days from order date)
  const estimatedDeliveryDate = new Date(order.orderDate);
  estimatedDeliveryDate.setDate(estimatedDeliveryDate.getDate() + 3);
  
  return (
    <div className="order-confirmation-page">
      <div className="container">
        <Breadcrumbs />
        <h1>Order Confirmation</h1>
        
        <div className="confirmation-content">
          <div className="confirmation-header">
            <h2>Thank you for your order!</h2>
            <p>Order #{order.orderId} has been placed successfully.</p>
          </div>
          
          <div className="order-details">
            <div className="order-info">
              <h3>Order Information</h3>
              <p><strong>Order Date:</strong> {new Date(order.orderDate).toLocaleDateString('en-US', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit'
              })}</p>
              <p><strong>Estimated Delivery:</strong> {estimatedDeliveryDate.toLocaleDateString('en-US', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit'
              })}</p>
              <p><strong>Status:</strong> {order.status}</p>
              <p><strong>Total Amount:</strong> ${order.totalAmount.toFixed(2)}</p>
            </div>
            
            <div className="shipping-info">
              <h3>Shipping Information</h3>
              <p>Your order will be shipped to the address provided during checkout.</p>
              <p><strong>Tracking:</strong> <a href={`/order-tracking/${order.orderId}`} className="tracking-link">Track your order</a></p>
            </div>
          </div>
          
          <div className="order-items">
            <h3>Order Items</h3>
            <div className="items-list">
              {order.orderItems.map(item => (
                <div key={item.orderItemId} className="item">
                  <div className="item-image">
                    <img src={item.product.imageUrl} alt={item.product.name} />
                  </div>
                  <div className="item-details">
                    <h4>{item.product.name}</h4>
                    <div className="item-attributes">
                      <span className="brand">{item.product.brand}</span>
                      <span className="size">Size: {item.product.size}</span>
                      <span className="color">Color: {item.product.color}</span>
                    </div>
                  </div>
                  <div className="item-quantity-price">
                    <p className="quantity">Quantity: {item.quantity}</p>
                    <p className="price">${(item.price * item.quantity).toFixed(2)}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
          
          <div className="confirmation-actions">
            <button onClick={() => void navigate('/')} className="btn-primary">
              Continue Shopping
            </button>
            <button onClick={() => void navigate('/order-history')} className="btn-secondary">
              View Order History
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default OrderConfirmationPage;