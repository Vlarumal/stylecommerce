// "use memo";
import {
  CardElement,
  useElements,
  useStripe,
} from '@stripe/react-stripe-js';
import {
  Modal,
  Form,
  Input,
  Card,
  Button,
  Alert,
  Row,
  Col,
  Divider,
} from 'antd';
import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';

import { useCart } from '../contexts/CartContext';
import { OrderService } from '../services/orderService';

import Breadcrumbs from './Breadcrumbs';

const CheckoutPage: React.FC = () => {
  const { cart, clearCart } = useCart();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [orderSuccess, setOrderSuccess] = useState(false);
  const [paymentStatus, setPaymentStatus] = useState<string | null>(
    null
  );
  const stripe = useStripe();
  const elements = useElements();
  const [requires3DSecure, setRequires3DSecure] = useState(false);
  const [redirectUrl, setRedirectUrl] = useState<string | null>(null);
  const [orderId, setOrderId] = useState<number | null>(null);

  const [shippingInfo, setShippingInfo] = useState({
    fullName: '',
    address: '',
    city: '',
    zipCode: '',
    country: '',
  });
  const location = useLocation();

  useEffect(() => {
    if (!cart || cart.cartItems.length === 0) {
      void navigate('/cart');
    }
  }, [cart, navigate]);

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const paymentIntentId = params.get('payment_intent');
    const redirectStatus = params.get('redirect_status');
    const orderId = params.get('order_id');

    if (paymentIntentId && redirectStatus) {
      if (redirectStatus === 'succeeded') {
        setPaymentStatus(
          '3D Secure authentication successful. Processing payment...'
        );
        // In a real implementation, you would confirm the payment with your backend
        // For this example, we'll just simulate success
        setTimeout(() => {
          setOrderSuccess(true);
          setPaymentStatus('Payment successful!');
          clearCart();
        }, 2000);
      } else {
        setError(
          '3D Secure authentication failed. Please try again.'
        );
        setPaymentStatus('Payment failed');
      }
    }

    const requires3DSecureReturn = params.get('requires_3d_secure');
    if (requires3DSecureReturn === 'true' && orderId) {
      setPaymentStatus('Completing 3D Secure authentication...');
      // In a real implementation, you would complete the 3D Secure authentication with your backend
      // For this example, we'll just simulate success
      setTimeout(() => {
        setOrderSuccess(true);
        setPaymentStatus('Payment successful!');
        clearCart();
      }, 2000);
    }
  }, [location.search, clearCart]);

  useEffect(() => {
    if (requires3DSecure && redirectUrl) {
      // Add a small delay before redirect to show the status message
      const timer = setTimeout(() => {
        window.location.href = redirectUrl;
      }, 2000);

      return () => clearTimeout(timer);
    }
  }, [requires3DSecure, redirectUrl]);

  const calculateSubtotal = () => {
    if (!cart) return 0;
    return cart.cartItems.reduce(
      (sum, item) => sum + item.priceSnapshot * item.quantity,
      0
    );
  };

  const calculateTax = () => {
    return calculateSubtotal() * 0.08; // 8% tax
  };

  const calculateTotal = () => {
    return calculateSubtotal() + calculateTax() + 15; // $15 shipping
  };

  const handlePlaceOrder = async (values: {
    fullName: string;
    address: string;
    city: string;
    zipCode: string;
    country: string;
  }) => {
    setLoading(true);
    setError(null);
    setPaymentStatus(null);
    setRequires3DSecure(false);
    setRedirectUrl(null);

    if (!stripe || !elements) {
      setError(
        'Payment system not initialized. Please refresh the page.'
      );
      setLoading(false);
      return;
    }

    try {
      setPaymentStatus('Processing payment...');

      const cardElement = elements.getElement('card');
      if (!cardElement) {
        throw new Error(
          'Payment form not properly loaded. Please refresh the page.'
        );
      }

      const { paymentMethod, error: paymentMethodError } =
        await stripe.createPaymentMethod({
          type: 'card',
          card: cardElement,
          billing_details: {
            name: values.fullName,
            address: {
              line1: values.address,
              city: values.city,
              postal_code: values.zipCode,
              country: values.country,
            },
          },
        });

      if (paymentMethodError) {
        throw new Error(
          paymentMethodError.message ||
            'Failed to create payment method'
        );
      }

      if (!paymentMethod) {
        throw new Error('Failed to create payment method');
      }

      const response = await OrderService.createOrder(
        paymentMethod.id
      );

      if (!response) {
        throw new Error('Failed to create order. Please try again.');
      }

      if (
        response.paymentResult.requires3DSecure &&
        response.paymentResult.redirectUrl
      ) {
        setRequires3DSecure(true);
        const redirectUrlWithOrderId = `${response.paymentResult.redirectUrl}?order_id=${response.order.orderId}`;
        setRedirectUrl(redirectUrlWithOrderId);
        setPaymentStatus(
          'Redirecting to 3D Secure authentication...'
        );

        setTimeout(() => {
          window.location.href = redirectUrlWithOrderId;
        }, 2000);
        return;
      }

      if (!response.paymentResult.isSuccess) {
        throw new Error(
          response.paymentResult.message || 'Payment failed'
        );
      }

      setOrderSuccess(true);
      setOrderId(response.order.orderId);
      setPaymentStatus(
        `Payment successful! Transaction ID: ${response.paymentResult.transactionId}`
      );
      clearCart();
    } catch (err: unknown) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to place order';
      setError(errorMessage);
      setPaymentStatus('Payment failed');

      console.error('Order placement error:', {
        message: errorMessage,
        stripeStatus: stripe ? 'loaded' : 'not loaded',
        elementsStatus: elements ? 'loaded' : 'not loaded',
      });

      setLoading(false);
    }
  };

  if (!cart || cart.cartItems.length === 0) {
    return (
      <div className='checkout-page'>
        <div className='container'>
          <Breadcrumbs />
          <h1>Checkout</h1>
          <p>
            Your cart is empty. <a href='/cart'>Go to cart</a>
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className='checkout-page'>
      <div className='container'>
        <Breadcrumbs />
        <h1>Checkout</h1>

        {orderSuccess ? (
          <div>
            <Modal
              open={orderSuccess}
              onCancel={() => {
                if (orderId) {
                  void navigate(`/order-confirmation/${orderId}`);
                } else {
                  setOrderSuccess(false);
                }
              }}
              title='Order Confirmation'
              footer={null}
              destroyOnHidden={true}
            >
              <div className='modal-content'>
                <h2>Order Placed Successfully!</h2>
                <p>
                  Thank you for your purchase. Your payment has been
                  processed successfully.
                </p>
                <Button
                  type='primary'
                  onClick={() => {
                    if (orderId) {
                      void navigate(`/order-confirmation/${orderId}`);
                    } else {
                      setOrderSuccess(false);
                    }
                  }}
                >
                  View Order Details
                </Button>
              </div>
            </Modal>
          </div>
        ) : (
          <div className='checkout-content'>
            <div className='checkout-form'>
              <Form
                onFinish={void handlePlaceOrder}
                layout='vertical'
              >
                <Card
                  title='Shipping Information'
                  style={{ marginBottom: 24 }}
                >
                  <Row gutter={16}>
                    <Col xs={24}>
                      <Form.Item
                        label='Full Name'
                        name='fullName'
                        rules={[
                          {
                            required: true,
                            message: 'Please enter your full name',
                          },
                          {
                            max: 100,
                            message:
                              'Full name is too long. Please enter a shorter name.',
                          },
                        ]}
                        initialValue={shippingInfo.fullName}
                      >
                        <Input
                          placeholder='Enter your full name'
                          onChange={(e) =>
                            setShippingInfo({
                              ...shippingInfo,
                              fullName: e.target.value,
                            })
                          }
                        />
                      </Form.Item>
                    </Col>
                  </Row>

                  <Row gutter={16}>
                    <Col xs={24}>
                      <Form.Item
                        label='Address'
                        name='address'
                        rules={[
                          {
                            required: true,
                            message: 'Please enter your address',
                          },
                          {
                            max: 200,
                            message:
                              'Address is too long. Please enter a shorter address.',
                          },
                        ]}
                        initialValue={shippingInfo.address}
                      >
                        <Input
                          placeholder='Enter your address'
                          onChange={(e) =>
                            setShippingInfo({
                              ...shippingInfo,
                              address: e.target.value,
                            })
                          }
                        />
                      </Form.Item>
                    </Col>
                  </Row>

                  <Row gutter={16}>
                    <Col
                      xs={24}
                      sm={12}
                    >
                      <Form.Item
                        label='City'
                        name='city'
                        rules={[
                          {
                            required: true,
                            message: 'Please enter your city',
                          },
                          {
                            max: 50,
                            message:
                              'City name is too long. Please enter a shorter city name.',
                          },
                        ]}
                        initialValue={shippingInfo.city}
                      >
                        <Input
                          placeholder='Enter your city'
                          onChange={(e) =>
                            setShippingInfo({
                              ...shippingInfo,
                              city: e.target.value,
                            })
                          }
                        />
                      </Form.Item>
                    </Col>

                    <Col
                      xs={24}
                      sm={12}
                    >
                      <Form.Item
                        label='ZIP Code'
                        name='zipCode'
                        rules={[
                          {
                            required: true,
                            message: 'Please enter your ZIP code',
                          },
                          {
                            max: 20,
                            message:
                              'ZIP code is too long. Please enter a valid ZIP code.',
                          },
                        ]}
                        initialValue={shippingInfo.zipCode}
                      >
                        <Input
                          placeholder='Enter your ZIP code'
                          onChange={(e) =>
                            setShippingInfo({
                              ...shippingInfo,
                              zipCode: e.target.value,
                            })
                          }
                        />
                      </Form.Item>
                    </Col>
                  </Row>

                  <Row gutter={16}>
                    <Col xs={24}>
                      <Form.Item
                        label='Country'
                        name='country'
                        rules={[
                          {
                            required: true,
                            message: 'Please enter your country',
                          },
                          {
                            max: 50,
                            message:
                              'Country name is too long. Please enter a valid country name.',
                          },
                        ]}
                        initialValue={shippingInfo.country}
                      >
                        <Input
                          placeholder='Enter your country'
                          onChange={(e) =>
                            setShippingInfo({
                              ...shippingInfo,
                              country: e.target.value,
                            })
                          }
                        />
                      </Form.Item>
                    </Col>
                  </Row>
                </Card>
                <Card
                  title='Payment Information'
                  style={{ marginBottom: 24 }}
                >
                  <Form.Item label='Credit or Debit Card'>
                    <CardElement
                      options={{
                        style: {
                          base: {
                            fontSize: '16px',
                            color: '#424770',
                            '::placeholder': {
                              color: '#aab7c4',
                            },
                          },
                        },
                      }}
                    />
                  </Form.Item>
                  {error && !paymentStatus && (
                    <Alert
                      message={error}
                      type='error'
                      showIcon
                    />
                  )}
                </Card>

                <Card
                  title='Order Summary'
                  style={{ marginBottom: 24 }}
                >
                  <div
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      marginBottom: 8,
                    }}
                  >
                    <span>Subtotal:</span>
                    <span>${calculateSubtotal().toFixed(2)}</span>
                  </div>
                  <div
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      marginBottom: 8,
                    }}
                  >
                    <span>Shipping:</span>
                    <span>$15.00</span>
                  </div>
                  <div
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      marginBottom: 8,
                    }}
                  >
                    <span>Tax:</span>
                    <span>${calculateTax().toFixed(2)}</span>
                  </div>
                  <Divider style={{ margin: '8px 0' }} />
                  <div
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      fontWeight: 'bold',
                    }}
                  >
                    <span>Total:</span>
                    <span>${calculateTotal().toFixed(2)}</span>
                  </div>
                </Card>

                {paymentStatus && (
                  <Alert
                    message={paymentStatus}
                    type={
                      paymentStatus.includes('successful')
                        ? 'success'
                        : paymentStatus.includes('failed')
                        ? 'error'
                        : 'info'
                    }
                    showIcon
                    style={{ marginBottom: 24 }}
                  />
                )}

                <Form.Item>
                  <Button
                    type='primary'
                    htmlType='submit'
                    size='large'
                    block
                    loading={loading}
                    disabled={
                      !stripe || !elements || requires3DSecure
                    }
                  >
                    {loading
                      ? 'Processing...'
                      : requires3DSecure
                      ? 'Redirecting to 3D Secure...'
                      : 'Place Order'}
                  </Button>
                </Form.Item>
              </Form>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default CheckoutPage;
