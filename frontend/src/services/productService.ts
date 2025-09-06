import { Product } from '../models/Product';

const API_BASE_URL = `${import.meta.env.VITE_BASE_URL}/api`;

export class ProductService {
  private static getAuthHeaders(): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    return headers;
  }

  static async getAllProducts(): Promise<Product[]> {
    const response = await fetch(`${API_BASE_URL}/product`, {
      headers: this.getAuthHeaders(),
    });
    if (!response.ok) {
      throw new Error('Failed to fetch products');
    }
    return response.json() as Promise<Product[]>;
  }

  static async getProductById(id: number): Promise<Product> {
    const response = await fetch(`${API_BASE_URL}/product/${id}`, {
      headers: this.getAuthHeaders(),
    });
    if (!response.ok) {
      throw new Error('Failed to fetch product');
    }
    return response.json() as Promise<Product>;
  }

  static async createProduct(
    product: Omit<Product, 'id' | 'createdAt' | 'updatedAt'>
  ): Promise<Product> {
    const response = await fetch(`${API_BASE_URL}/product`, {
      method: 'POST',
      headers: this.getAuthHeaders(),
      body: JSON.stringify(product),
    });
    if (!response.ok) {
      throw new Error('Failed to create product');
    }
    return response.json() as Promise<Product>;
  }

  static async updateProduct(product: Product): Promise<Product> {
    const response = await fetch(
      `${API_BASE_URL}/product/${product.id}`,
      {
        method: 'PUT',
        headers: this.getAuthHeaders(),
        body: JSON.stringify(product),
      }
    );
    if (!response.ok) {
      throw new Error('Failed to update product');
    }
    return response.json() as Promise<Product>;
  }

  static async deleteProduct(id: number): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/product/${id}`, {
      method: 'DELETE',
      headers: this.getAuthHeaders(),
    });
    if (!response.ok) {
      throw new Error('Failed to delete product');
    }
  }

  static async verifyProduct(
    id: number,
    isVerified: boolean
  ): Promise<void> {
    const product = await this.getProductById(id);
    const updatedProduct = { ...product, isVerified };
    await this.updateProduct(updatedProduct);
  }

  static async getProductsByCategory(
    categoryId: number
  ): Promise<Product[]> {
    const response = await fetch(
      `${API_BASE_URL}/product/category/${categoryId}`,
      {
        headers: this.getAuthHeaders(),
      }
    );
    if (!response.ok) {
      throw new Error('Failed to fetch products by category');
    }
    return response.json() as Promise<Product[]>;
  }

  static async searchProducts(query: string): Promise<Product[]> {
    const response = await fetch(
      `${API_BASE_URL}/product/search?query=${encodeURIComponent(
        query
      )}`,
      {
        headers: this.getAuthHeaders(),
      }
    );
    if (!response.ok) {
      throw new Error('Failed to search products');
    }
    return response.json() as Promise<Product[]>;
  }
}
