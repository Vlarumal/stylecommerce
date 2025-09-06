const API_BASE_URL = `${import.meta.env.VITE_BASE_URL}/api`;

interface Product {
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
}

interface OrderItem {
  orderItemId: number;
  orderId: number;
  productId: number;
  quantity: number;
  price: number;
  product: Product;
}

interface Order {
  orderId: number;
  userId: number;
  orderDate: string;
  totalAmount: number;
  status: string;
  orderItems: OrderItem[];
}

interface PaymentResult {
  isSuccess: boolean;
  transactionId: string;
  message: string;
  amount: number;
  paymentMethod: string;
  processedAt: string;
  requires3DSecure?: boolean;
  redirectUrl?: string;
}

interface CreateOrderResponse {
  order: Order;
  paymentResult: PaymentResult;
}

class RateLimiter {
  private lastRequestTime: number = 0;
  private minInterval: number = 5000; // 5 seconds minimum between requests

  canMakeRequest(): boolean {
    const now = Date.now();
    if (now - this.lastRequestTime >= this.minInterval) {
      this.lastRequestTime = now;
      return true;
    }
    return false;
  }

  getTimeUntilNextRequest(): number {
    const now = Date.now();
    return Math.max(
      0,
      this.minInterval - (now - this.lastRequestTime)
    );
  }
}

const paymentRateLimiter = new RateLimiter();

export class OrderService {
  private static getAuthHeaders(): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      // Security header to prevent CSRF attacks
      'X-Requested-With': 'XMLHttpRequest',
    };

    return headers;
  }

  static async createOrder(
    paymentToken: string
  ): Promise<CreateOrderResponse> {
    if (!paymentRateLimiter.canMakeRequest()) {
      const timeUntilNextRequest =
        paymentRateLimiter.getTimeUntilNextRequest();
      throw new Error(
        `Please wait ${Math.ceil(
          timeUntilNextRequest / 1000
        )} seconds before making another payment request.`
      );
    }

    // Send the payment token directly without sanitization
    // Stripe payment method IDs have a specific format that should not be modified
    const response = await fetch(`${API_BASE_URL}/order/create`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify({ paymentToken }),
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to create order: ${errorText}`);
    }

    return response.json() as Promise<CreateOrderResponse>;
  }

  static async getOrderHistory(): Promise<Order[]> {
    const response = await fetch(`${API_BASE_URL}/order/history`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch order history');
    }

    return response.json() as Promise<Order[]>;
  }

  static async getOrderDetails(orderId: number): Promise<Order> {
    const response = await fetch(`${API_BASE_URL}/order/${orderId}`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to fetch order details: ${errorText}`);
    }

    return response.json() as Promise<Order>;
  }

  static async updateOrderStatus(
    orderId: number,
    status: string
  ): Promise<Order> {
    const response = await fetch(
      `${API_BASE_URL}/order/${orderId}/status`,
      {
        method: 'PUT',
        headers: this.getAuthHeaders(),
        body: JSON.stringify({ status }),
      }
    );

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to update order status: ${errorText}`);
    }

    return response.json() as Promise<Order>;
  }

  static async getAvailableOrderStatuses(): Promise<string[]> {
    const response = await fetch(`${API_BASE_URL}/order/statuses`, {
      headers: this.getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch available order statuses');
    }

    return response.json() as Promise<string[]>;
  }
}
