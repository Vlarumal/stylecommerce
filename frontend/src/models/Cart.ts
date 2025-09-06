export interface CartItem {
    id: number;
    cartId: number;
    productId: number;
    quantity: number;
    priceSnapshot: number;
    addedDate: string;
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

export interface Cart {
    id: number;
    userId: number;
    createdDate: string;
    cartItems: CartItem[];
}