import { Breadcrumb } from 'antd';
import type { BreadcrumbProps } from 'antd';
import React from 'react';
import { useLocation, Link } from 'react-router-dom';

interface BreadcrumbsProps {
  productName?: string;
}

const Breadcrumbs: React.FC<BreadcrumbsProps> = ({ productName }) => {
  const location = useLocation();
  
  const pathnames = location.pathname.split('/').filter(x => x);
  
  const breadcrumbLabels: Record<string, string> = {
    '': 'Home',
    'product': 'Product',
    'cart': 'Cart',
    'checkout': 'Checkout',
    'order-confirmation': 'Order Confirmation',
    'order-history': 'Order History',
    'order-details': 'Order Details',
    'admin': 'Admin',
    'orders': 'Orders',
    'login': 'Login'
  };

  const breadcrumbItems: BreadcrumbProps['items'] = [];
  
  breadcrumbItems.push({
    title: <Link to="/">Home</Link>,
  });

  let filteredIndex = 0;
  for (let i = 0; i < pathnames.length; i++) {
    const pathname = pathnames[i];
    
    // Skip the 'product' segment since there's no route for it
    if (pathname === 'product') {
      continue;
    }
    
    filteredIndex++;
    
    // Construct the route up to this point (including skipped segments)
    const routeTo = `/${pathnames.slice(0, i + 1).join('/')}`;
    
    const isLast = filteredIndex === pathnames.filter(p => p !== 'product').length;
    
    let label = breadcrumbLabels[pathname] || pathname;
    
    // If this is a product detail page, we might want to show the product name
    // Check if the previous segment is 'product' and this segment is a number
    if (i > 0 && pathnames[i - 1] === 'product' && /^\d+$/.test(pathname)) {
      label = productName || 'Product Details';
    }
    
    breadcrumbItems.push({
      title: isLast ? label : <Link to={routeTo}>{label}</Link>,
    });
  }

  return <Breadcrumb items={breadcrumbItems} />;
};

export default Breadcrumbs;