import { Button as AntButton, ButtonProps as AntButtonProps } from 'antd';
import type { ButtonType } from 'antd/es/button';
import React from 'react';

import styles from './Button.module.css';

interface ButtonProps extends Omit<AntButtonProps, 'size'> {
  buttonStyle?: 'primary' | 'secondary' | 'outline';
  size?: 'sm' | 'md' | 'lg';
  ariaLabel: string;
}

const Button: React.FC<ButtonProps> = ({
  buttonStyle = 'primary',
  size = 'md',
  onClick,
  children,
  ariaLabel,
  className,
  ...rest
}) => {
  // Map custom buttonStyle to Ant Design types
  const getType = (): ButtonType => {
    switch (buttonStyle) {
      case 'primary':
        return 'primary';
      case 'secondary':
        return 'default';
      case 'outline':
        return 'default'; // We'll handle outline styling with ghost prop
      default:
        return 'default';
    }
  };

  // Map custom sizes to Ant Design sizes
  const getSize = () => {
    switch (size) {
      case 'sm':
        return 'small';
      case 'lg':
        return 'large';
      default:
        return 'middle';
    }
  };

  // For outline style, we use ghost prop
  const isGhost = buttonStyle === 'outline';

  // Combine custom styles with Ant Design styles
  const getCustomClass = () => {
    switch (buttonStyle) {
      case 'primary':
        return styles.primary;
      case 'secondary':
        return styles.secondary;
      case 'outline':
        return styles.outline;
      default:
        return '';
    }
  };

  return (
    <AntButton
      type={getType()}
      size={getSize()}
      ghost={isGhost}
      onClick={onClick}
      aria-label={ariaLabel}
      className={`${getCustomClass()} ${className || ''}`}
      {...rest}
    >
      {children}
    </AntButton>
  );
};

export default Button;
