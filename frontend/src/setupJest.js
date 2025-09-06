global.import = {
  meta: {
    env: process.env
  }
};

if (typeof window !== 'undefined') {
  window.import = {
    meta: {
      env: process.env
    }
  };
}

process.env.NODE_ENV = 'test';
process.env.VITE_API_BASE_URL = 'http://localhost:5209';
process.env.VITE_STRIPE_PUBLISHABLE_KEY = 'pk_test_123';