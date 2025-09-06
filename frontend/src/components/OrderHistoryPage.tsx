import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

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

const OrderHistoryPage: React.FC = () => {
  const navigate = useNavigate();
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchOrderHistory = async () => {
      try {
        const orderHistory = await OrderService.getOrderHistory();
        setOrders(orderHistory);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to fetch order history');
        console.error('Error fetching order history:', err);
      } finally {
        setLoading(false);
      }
    };

    void fetchOrderHistory();
  }, []);

  const handleViewDetails = (orderId: number) => {
    void navigate(`/order-details/${orderId}`);
  };

  if (loading) {
    return (
      <div className="order-history-page">
        <div className="container">
          <h1>Order History</h1>
          <p>Loading your order history...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="order-history-page">
        <div className="container">
          <h1>Order History</h1>
          <div className="error-message">
            <p>{error}</p>
            <button onClick={() => window.location.reload()} className="btn-primary">
              Try Again
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="order-history-page">
      <div className="container">
        <h1>Order History</h1>
        
        {orders.length === 0 ? (
          <div className="empty-history">
            <p>You haven't placed any orders yet.</p>
            <button onClick={() => void navigate('/')} className="btn-primary">
              Start Shopping
            </button>
          </div>
        ) : (
          <div className="orders-list">
            {orders.map(order => (
              <div key={order.orderId} className="order-card">
                <div className="order-header">
                  <div className="order-info">
                    <h3>Order #{order.orderId}</h3>
                    <p className="order-date">
                      {new Date(order.orderDate).toLocaleDateString()}
                    </p>
                  </div>
                  <div className="order-status">
                    <span className={`status ${order.status.toLowerCase()}`}>
                      {order.status}
                    </span>
                  </div>
                </div>
                
                <div className="order-summary">
                  <div className="order-items-count">
                    <p>{order.orderItems.length} items</p>
                  </div>
                  <div className="order-total">
                    <p><strong>Total: ${order.totalAmount.toFixed(2)}</strong></p>
                  </div>
                </div>
                
                <div className="order-actions">
                  <button 
                    onClick={() => handleViewDetails(order.orderId)}
                    className="btn-secondary"
                  >
                    View Details
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default OrderHistoryPage;