import { Card as AntCard, CardProps as AntCardProps } from 'antd';
import React from 'react';

interface CardProps extends AntCardProps {
  children: React.ReactNode;
  className?: string;
}

const Card = React.forwardRef<HTMLDivElement, CardProps>(({
  children,
  className = '',
  ...rest
}, ref) => {
  return (
    <AntCard
      ref={ref}
      className={className}
      {...rest}
    >
      {children}
    </AntCard>
  );
});

Card.displayName = 'Card';

export default Card;