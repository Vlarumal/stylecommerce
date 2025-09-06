import { Cart, CartItem } from '../models/Cart';

const API_BASE_URL = `${import.meta.env.VITE_BASE_URL}/api`;

interface AddToCartRequest {
  productId: number;
  quantity: number;
}

interface UpdateQuantityRequest {
  productId: number;
  quantity: number;
}

export class CartService {
  private static getAuthHeaders(): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    return headers;
  }

  static async getCart(): Promise<Cart> {
    const response = await fetch(`${API_BASE_URL}/cart`, {
      headers: this.getAuthHeaders(),
      credentials: 'include',
    });

    if (!response.ok) {
      throw new Error('Failed to fetch cart');
    }

    return response.json() as Promise<Cart>;
  }

  static async addToCart(
    productId: number,
    quantity: number = 1
  ): Promise<CartItem> {
    const request: AddToCartRequest = { productId, quantity };

    const response = await fetch(`${API_BASE_URL}/cart/add`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(request),
      credentials: 'include',
    });

    if (!response.ok) {
      throw new Error('Failed to add item to cart');
    }

    return response.json() as Promise<CartItem>;
  }

  static async updateCartItem(
    productId: number,
    newQuantity: number
  ): Promise<CartItem> {
    const request: UpdateQuantityRequest = {
      productId,
      quantity: newQuantity,
    };

    const response = await fetch(`${API_BASE_URL}/cart/update`, {
      method: 'PUT',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(request),
      credentials: 'include',
    });

    if (!response.ok) {
      throw new Error('Failed to update cart item');
    }

    return response.json() as Promise<CartItem>;
  }

  static async removeCartItem(productId: number): Promise<void> {
    try {
      const response = await fetch(
        `${API_BASE_URL}/cart/remove?productId=${productId}`,
        {
          method: 'DELETE',
          headers: this.getAuthHeaders(),
          credentials: 'include',
        }
      );

      if (response.status === 204) return;

      if (!response.ok) {
        const errorData = (await response.json()) as Error;
        throw new Error(
          errorData.message || 'Failed to remove item from cart'
        );
      }
    } catch (error) {
      console.error(`Error removing cart item ${productId}:`, error);
      throw error;
    }
  }
}
