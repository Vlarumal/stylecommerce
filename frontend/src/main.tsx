import { Elements } from '@stripe/react-stripe-js';
import { loadStripe } from '@stripe/stripe-js';
import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';

import App from './App';
import './App.css';
import 'antd/dist/reset.css';

const stripePublishableKey = import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY as string;

if (!stripePublishableKey) {
  console.error('Stripe publishable key is missing. Please check your environment variables.');
  throw new Error('Stripe publishable key is missing. Please check your environment variables.');
}

const stripePromise = loadStripe(stripePublishableKey);

const root = ReactDOM.createRoot(
    document.getElementById('root') as HTMLElement
);

root.render(
    <React.StrictMode>
        <BrowserRouter>
            <Elements stripe={stripePromise}>
                <App />
            </Elements>
        </BrowserRouter>
    </React.StrictMode>
);
