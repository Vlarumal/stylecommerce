import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';

interface AuthRedirectWrapperProps {
  isLoggedIn: boolean;
  element: React.ReactNode;
}

const AuthRedirectWrapper: React.FC<AuthRedirectWrapperProps> = ({ isLoggedIn, element }) => {
  const location = useLocation();
  
  if (isLoggedIn) {
    return <>{element}</>;
  }

  return <Navigate to='/login' state={{ from: location }} replace />;
};

export default AuthRedirectWrapper;