// "use memo";
import { Badge } from 'antd';
import React, { KeyboardEvent } from 'react';
import { useNavigate } from 'react-router-dom';

import { useCart } from '../contexts/CartContext';
import './CartIcon.css';

const CartIcon: React.FC = () => {
    const { cartItemsCount, loading } = useCart();
    const navigate = useNavigate();

    const handleClick = () => {
        void navigate('/cart');
    };

    const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
        if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault(); // Prevent space from scrolling the page
            handleClick();
        }
    };

    const cartLabel = `Shopping cart with ${loading ? 0 : cartItemsCount} items`;

    return (
        <div
            className="cart-icon"
            onClick={handleClick}
            onKeyDown={handleKeyDown}
            role="button"
            tabIndex={0}
            aria-label={cartLabel}
        >
            <Badge count={loading ? 0 : cartItemsCount} showZero={false}>
                <span className="cart-icon__icon">🛒</span>
            </Badge>
        </div>
    );
};

export default CartIcon;