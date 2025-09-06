// import { AnalyticsBrowser } from '@segment/analytics-next';

// // Initialize Segment analytics client
// const analytics = AnalyticsBrowser.load({
//   writeKey: import.meta.env.VITE_SEGMENT_WRITE_KEY
// });

export const AnalyticsService = {
  trackEvent: (eventName: string) => {
    console.log(`Analytics Event: ${eventName}`);
  },
  trackAREngagement: (productId: number, productName: string) => {
    console.log(`Analytics Event: AR Engagement - Product ID: ${productId}, Name: ${productName}`);
  },
  trackVerificationBadge: (productId: number, isVerified: boolean) => {
    console.log(`Analytics Event: Verification Badge - Product ID: ${productId}, Verified: ${isVerified}`);
  }
  // trackEvent: (eventName: string, properties: Record<string, any> = {}) => {
  //   analytics.track(eventName, {
  //     ...properties,
  //     timestamp: new Date().toISOString()
  //   });
  // },
  
  // // Track AR try-on engagement
  // trackAREngagement: (productId: number, productName: string) => {
  //   analytics.track('AR Try-On Engaged', {
  //     productId,
  //     productName,
  //     timestamp: new Date().toISOString()
  //   });
  // },
  
  // // Track verification badge interaction
  // trackVerificationBadge: (productId: number, isVerified: boolean) => {
  //   analytics.track('Verification Badge Viewed', {
  //     productId,
  //     isVerified,
  //     timestamp: new Date().toISOString()
  //   });
  // }
};