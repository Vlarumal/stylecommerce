import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

import { OrderService } from '../../services/orderService';

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

const OrderManagementPage: React.FC = () => {
  const navigate = useNavigate();
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [availableStatuses, setAvailableStatuses] = useState<
    string[]
  >([]);
  const [updatingOrderId, setUpdatingOrderId] = useState<
    number | null
  >(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const orderHistory = await OrderService.getOrderHistory();
        setOrders(orderHistory);

        const statuses =
          await OrderService.getAvailableOrderStatuses();
        setAvailableStatuses(statuses);
      } catch (err) {
        setError(
          err instanceof Error ? err.message : 'Failed to fetch data'
        );
        console.error('Error fetching data:', err);
      } finally {
        setLoading(false);
      }
    };

    void fetchData();
  }, []);

  const handleUpdateStatus = async (
    orderId: number,
    newStatus: string
  ) => {
    try {
      setUpdatingOrderId(orderId);
      const updatedOrder = await OrderService.updateOrderStatus(
        orderId,
        newStatus
      );

      setOrders((prevOrders) =>
        prevOrders.map((order) =>
          order.orderId === orderId ? updatedOrder : order
        )
      );
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Failed to update order status'
      );
      console.error('Error updating order status:', err);
    } finally {
      setUpdatingOrderId(null);
    }
  };

  const handleViewDetails = (orderId: number) => {
    void navigate(`/order-details/${orderId}`);
  };

  if (loading) {
    return (
      <div className='order-management-page'>
        <div className='container'>
          <h1>Order Management</h1>
          <p>Loading orders...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className='order-management-page'>
        <div className='container'>
          <h1>Order Management</h1>
          <div className='error-message'>
            <p>{error}</p>
            <button
              onClick={() => window.location.reload()}
              className='btn-primary'
            >
              Try Again
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className='order-management-page'>
      <div className='container'>
        <div className='page-header'>
          <h1>Order Management</h1>
        </div>

        {orders.length === 0 ? (
          <div className='empty-orders'>
            <p>No orders found.</p>
          </div>
        ) : (
          <div className='orders-list'>
            {orders.map((order) => (
              <div
                key={order.orderId}
                className='order-card'
              >
                <div className='order-header'>
                  <div className='order-info'>
                    <h3>Order #{order.orderId}</h3>
                    <p className='order-date'>
                      {new Date(order.orderDate).toLocaleDateString()}
                    </p>
                  </div>
                  <div className='order-status'>
                    <span
                      className={`status ${order.status.toLowerCase()}`}
                    >
                      {order.status}
                    </span>
                  </div>
                </div>

                <div className='order-summary'>
                  <div className='order-items-count'>
                    <p>{order.orderItems.length} items</p>
                  </div>
                  <div className='order-total'>
                    <p>
                      <strong>
                        Total: ${order.totalAmount.toFixed(2)}
                      </strong>
                    </p>
                  </div>
                </div>

                <div className='order-actions'>
                  <div className='status-update'>
                    <label htmlFor={`status-${order.orderId}`}>
                      Update Status:
                    </label>
                    <select
                      id={`status-${order.orderId}`}
                      value={order.status}
                      onChange={(e) =>
                        void handleUpdateStatus(
                          order.orderId,
                          e.target.value
                        )
                      }
                      disabled={updatingOrderId === order.orderId}
                      className='status-select'
                    >
                      {availableStatuses.map((status) => (
                        <option
                          key={status}
                          value={status}
                        >
                          {status}
                        </option>
                      ))}
                    </select>
                    {updatingOrderId === order.orderId && (
                      <span className='updating-indicator'>
                        Updating...
                      </span>
                    )}
                  </div>

                  <button
                    onClick={() => handleViewDetails(order.orderId)}
                    className='btn-secondary'
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

export default OrderManagementPage;
