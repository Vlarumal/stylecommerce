// "use memo";
import React, {
  createContext,
  useContext,
  useEffect,
  ReactNode,
  useCallback,
  useReducer,
} from 'react';

import { Cart } from '../models/Cart';
import { CartService } from '../services/cartService';

interface CartContextType {
  cart: Cart | null;
  cartItemsCount: number;
  loading: boolean;
  error: string | null;
  fetchCart: () => Promise<void>;
  addToCart: (productId: number, quantity: number) => Promise<void>;
  updateCartItem: (
    productId: number,
    newQuantity: number
  ) => Promise<void>;
  removeCartItem: (productId: number) => Promise<void>;
  clearCart: () => void;
  mergeGuestCart: () => Promise<void>;
}

const CartContext = createContext<CartContextType | undefined>(
  undefined
);

interface CartProviderProps {
  children: ReactNode;
}

export const CartProvider: React.FC<CartProviderProps> = ({
  children,
}) => {
  const [state, dispatch] = useReducer(cartReducer, {
    cart: null,
    loading: true,
    error: null,
  });

  const { cart, loading, error } = state;

  useEffect(() => {
    const savedGuestCart = localStorage.getItem('guestCart');
    if (savedGuestCart) {
      try {
        const parsedCart: Cart = JSON.parse(savedGuestCart) as Cart;
        dispatch({ type: 'FETCH_SUCCESS', payload: parsedCart });
      } catch (error) {
        console.error(
          'Failed to parse guest cart from localStorage',
          error
        );
      }
    }
  }, []);

  useEffect(() => {
    const initializeCart = async () => {
      dispatch({ type: 'FETCH_REQUEST' });
      try {
        const fetchedCart = await CartService.getCart();
        dispatch({ type: 'FETCH_SUCCESS', payload: fetchedCart });
      } catch (err) {
        dispatch({
          type: 'FETCH_FAILURE',
          payload: 'Error fetching cart',
        });
        console.error('Error fetching cart:', err);
      }
    };

    void initializeCart();
  }, []);

  useEffect(() => {
    if (cart) {
      localStorage.setItem('guestCart', JSON.stringify(cart));
    } else {
      localStorage.removeItem('guestCart');
    }
  }, [cart]);

  const fetchCart = async () => {
    dispatch({ type: 'FETCH_REQUEST' });
    try {
      const fetchedCart = await CartService.getCart();
      dispatch({ type: 'FETCH_SUCCESS', payload: fetchedCart });
    } catch (err) {
      dispatch({
        type: 'FETCH_FAILURE',
        payload: 'Failed to fetch cart',
      });
      console.error('Error fetching cart:', err);
    }
  };

  const addToCart = useCallback(
    async (productId: number, quantity: number) => {
      dispatch({
        type: 'ADD_ITEM_START',
        payload: { productId, quantity },
      });
      try {
        await CartService.addToCart(productId, quantity);
        const fetchedCart = await CartService.getCart();
        dispatch({ type: 'FETCH_SUCCESS', payload: fetchedCart });
      } catch (err) {
        dispatch({
          type: 'FETCH_FAILURE',
          payload: 'Failed to add item to cart',
        });
        console.error('Error adding to cart:', err);
      }
    },
    []
  );

  const updateCartItem = useCallback(
    async (productId: number, newQuantity: number) => {
      dispatch({
        type: 'UPDATE_ITEM_START',
        payload: { productId, newQuantity },
      });
      try {
        await CartService.updateCartItem(productId, newQuantity);
        const fetchedCart = await CartService.getCart();
        dispatch({ type: 'FETCH_SUCCESS', payload: fetchedCart });
      } catch (err) {
        dispatch({
          type: 'FETCH_FAILURE',
          payload: 'Failed to update cart item',
        });
        console.error('Error updating cart item:', err);
      }
    },
    []
  );

  const removeCartItem = useCallback(
    async (productId: number) => {
      const currentCart = state.cart;

      dispatch({ type: 'REMOVE_ITEM_START', payload: productId });

      try {
        await CartService.removeCartItem(productId);
        const fetchedCart = await CartService.getCart();
        dispatch({ type: 'FETCH_SUCCESS', payload: fetchedCart });
      } catch (err) {
        const errorMessage =
          err instanceof Error
            ? err.message
            : 'Failed to remove item from cart';
        if (currentCart) {
          dispatch({ type: 'REVERT_CART', payload: currentCart });
        }
        dispatch({ type: 'FETCH_FAILURE', payload: errorMessage });
        console.error('Error removing from cart:', err);
      }
    },
    [state.cart]
  );

  const clearCart = () => {
    dispatch({ type: 'CLEAR_CART' });
    localStorage.removeItem('guestCart');
  };

  const mergeGuestCart = useCallback(async () => {
    const savedGuestCart = localStorage.getItem('guestCart');
    if (!savedGuestCart) return;

    try {
      const guestCart: Cart = JSON.parse(savedGuestCart) as Cart;
      dispatch({ type: 'FETCH_REQUEST' });

      // Add all items from guest cart to authenticated cart
      for (const item of guestCart.cartItems) {
        try {
          await CartService.addToCart(item.productId, item.quantity);
        } catch (err) {
          console.error(
            `Failed to merge cart item ${item.productId}`,
            err
          );
        }
      }

      const fetchedCart = await CartService.getCart();
      dispatch({ type: 'FETCH_SUCCESS', payload: fetchedCart });
      localStorage.removeItem('guestCart');
    } catch (error) {
      console.error('Failed to merge guest cart', error);
    }
  }, []);

  const cartItemsCount =
    cart?.cartItems.reduce(
      (total: number, item) => total + item.quantity,
      0
    ) || 0;

  function cartReducer(
    state: CartState,
    action: CartAction
  ): CartState {
    switch (action.type) {
      case 'FETCH_REQUEST':
        return { ...state, loading: true, error: null };
      case 'FETCH_SUCCESS':
        return { ...state, loading: false, cart: action.payload };
      case 'FETCH_FAILURE':
        return { ...state, loading: false, error: action.payload };
      case 'CLEAR_CART':
        localStorage.removeItem('guestCart');
        return { ...state, cart: null };
      case 'REMOVE_ITEM_START': {
        if (!state.cart) return state;
        const updatedItems = state.cart.cartItems.filter(
          (item) => item.productId !== action.payload
        );
        return {
          ...state,
          loading: true,
          error: null, // Clear previous errors
          cart: {
            ...state.cart,
            cartItems: updatedItems,
          },
        };
      }
      case 'REVERT_CART':
        return {
          ...state,
          cart: action.payload,
        };
      case 'CLEAR_ERROR':
        return {
          ...state,
          error: null,
        };
      case 'ADD_ITEM_START':
      case 'UPDATE_ITEM_START':
        return {
          ...state,
          loading: true,
          error: null,
        };
      default:
        return state;
    }
  }

  const value = {
    cart,
    cartItemsCount,
    loading,
    error,
    fetchCart,
    addToCart,
    updateCartItem,
    removeCartItem,
    clearCart,
    mergeGuestCart,
    clearError: () => dispatch({ type: 'CLEAR_ERROR' }),
  };

  return (
    <CartContext.Provider value={value}>
      {children}
    </CartContext.Provider>
  );
};

export const useCart = () => {
  const context = useContext(CartContext);
  if (context === undefined) {
    throw new Error('useCart must be used within a CartProvider');
  }
  return context;
};

interface CartState {
  cart: Cart | null;
  loading: boolean;
  error: string | null;
}

type CartAction =
  | { type: 'FETCH_REQUEST' }
  | { type: 'FETCH_SUCCESS'; payload: Cart }
  | { type: 'FETCH_FAILURE'; payload: string }
  | { type: 'CLEAR_CART' }
  | { type: 'REMOVE_ITEM_START'; payload: number }
  | { type: 'REVERT_CART'; payload: Cart }
  | { type: 'CLEAR_ERROR' }
  | {
      type: 'ADD_ITEM_START';
      payload: { productId: number; quantity: number };
    }
  | {
      type: 'UPDATE_ITEM_START';
      payload: { productId: number; newQuantity: number };
    };
