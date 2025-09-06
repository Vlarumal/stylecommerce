import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

import { OrderService } from '../services/orderService';

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

const OrderDetailsPage: React.FC = () => {
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

  const calculateSubtotal = () => {
    if (!order) return 0;
    return order.orderItems.reduce((sum, item) => sum + (item.price * item.quantity), 0);
  };

  const calculateTax = () => {
    return calculateSubtotal() * 0.08; // 8% tax
  };

  const calculateShipping = () => {
    return 15; // $15 shipping
  };

  if (loading) {
    return (
      <div className="order-details-page">
        <div className="container">
          <h1>Order Details</h1>
          <p>Loading order details...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="order-details-page">
        <div className="container">
          <h1>Order Details</h1>
          <div className="error-message">
            <p>{error}</p>
            <button onClick={() => void navigate('/order-history')} className="btn-primary">
              Back to Order History
            </button>
          </div>
        </div>
      </div>
    );
  }

  if (!order) {
    return (
      <div className="order-details-page">
        <div className="container">
          <h1>Order Details</h1>
          <p>Order not found.</p>
          <button onClick={() => void navigate('/order-history')} className="btn-primary">
            Back to Order History
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="order-details-page">
      <div className="container">
        <div className="page-header">
          <h1>Order Details</h1>
          <button onClick={() => void navigate('/order-history')} className="btn-secondary">
            Back to Order History
          </button>
        </div>
        
        <div className="order-details-content">
          <div className="order-header">
            <div className="order-info">
              <h2>Order #{order.orderId}</h2>
              <p className="order-date">
                Placed on {new Date(order.orderDate).toLocaleDateString()}
              </p>
              <p className="order-status">
                Status: <span className={`status ${order.status.toLowerCase()}`}>{order.status}</span>
              </p>
            </div>
          </div>
          
          <div className="order-items">
            <h3>Items in this Order</h3>
            <div className="items-list">
              {order.orderItems.map(item => (
                <div key={item.orderItemId} className="item">
                  <div className="item-image">
                    <img src={item.product.imageUrl} alt={item.product.name} />
                  </div>
                  <div className="item-details">
                    <h4>{item.product.name}</h4>
                    <p>{item.product.description}</p>
                    <p><strong>Brand:</strong> {item.product.brand}</p>
                    <p><strong>Size:</strong> {item.product.size}</p>
                    <p><strong>Color:</strong> {item.product.color}</p>
                  </div>
                  <div className="item-quantity">
                    <p>Quantity: {item.quantity}</p>
                  </div>
                  <div className="item-price">
                    <p>${item.price.toFixed(2)} each</p>
                    <p><strong>${(item.price * item.quantity).toFixed(2)}</strong></p>
                  </div>
                </div>
              ))}
            </div>
          </div>
          
          <div className="order-summary">
            <h3>Order Summary</h3>
            <div className="summary-details">
              <div className="summary-item">
                <span>Subtotal:</span>
                <span>${calculateSubtotal().toFixed(2)}</span>
              </div>
              <div className="summary-item">
                <span>Shipping:</span>
                <span>${calculateShipping().toFixed(2)}</span>
              </div>
              <div className="summary-item">
                <span>Tax:</span>
                <span>${calculateTax().toFixed(2)}</span>
              </div>
              <div className="summary-item total">
                <span><strong>Total:</strong></span>
                <span><strong>${order.totalAmount.toFixed(2)}</strong></span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default OrderDetailsPage;