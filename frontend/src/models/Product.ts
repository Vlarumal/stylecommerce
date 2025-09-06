export interface Product {
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