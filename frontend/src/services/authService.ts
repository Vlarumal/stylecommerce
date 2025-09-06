const API_BASE_URL = `${import.meta.env.VITE_BASE_URL}/api`;

// Constants for session storage keys
const REDIRECT_URL_KEY = 'auth_redirect_url';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
}

export interface RefreshTokenResponse {
  success: boolean;
}

export interface CheckAuthResponse {
  isAuthenticated: boolean;
  username?: string;
  userId?: string;
  role?: string;
}

interface LocationState {
  from?: {
    pathname?: string;
  };
}

// Utility functions for redirect URL management
export const RedirectService = {
  /**
   * Save the current URL for post-login redirection
   * @param url - The URL to save for redirection
   */
  saveRedirectUrl: (url: string): void => {
    // Don't save redirect URL for login page itself
    if (url && url !== '/login') {
      sessionStorage.setItem(REDIRECT_URL_KEY, url);
    }
  },

  /**
   * Get the saved redirect URL
   * @returns The saved URL or null if not found
   */
  getRedirectUrl: (): string | null => {
    const savedUrl = sessionStorage.getItem(REDIRECT_URL_KEY);
    return savedUrl;
  },

  /**
   * Clear the saved redirect URL
   */
  clearRedirectUrl: (): void => {
    sessionStorage.removeItem(REDIRECT_URL_KEY);
  },

  /**
   * Get redirect URL with fallback to default
   * @param defaultUrl - Fallback URL if no redirect URL is saved
   * @returns URL to redirect to
   */
  getRedirectUrlWithFallback: (defaultUrl: string = '/'): string => {
    const redirectUrl = RedirectService.getRedirectUrl();
    const result = redirectUrl || defaultUrl;
    return result;
  },
};

export const getCsrfToken = (): string | null => {
  const cookieValue = document.cookie
    .split('; ')
    .find((row) => row.startsWith('XSRF-TOKEN='))
    ?.split('=')[1];
  return cookieValue ? decodeURIComponent(cookieValue) : null;
};

export class AuthService {
  private static refreshTimeout: number | null = null;

  static async login(
    credentials: LoginRequest
  ): Promise<LoginResponse> {
    const csrfToken = getCsrfToken();
    const response = await fetch(`${API_BASE_URL}/account/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-XSRF-TOKEN': csrfToken || '',
      },
      body: JSON.stringify(credentials),
      credentials: 'include',
    });

    if (!response.ok) {
      throw new Error('Login failed');
    }

    const data = (await response.json()) as LoginResponse;

    // Schedule token refresh (30 minutes - 1 minute)
    this.scheduleTokenRefresh(29 * 60);

    return {
      success: data.success,
    };
  }

  static async refreshToken(): Promise<RefreshTokenResponse> {
    const csrfToken = getCsrfToken();

    const response = await fetch(
      `${API_BASE_URL}/account/refresh-token`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-XSRF-TOKEN': csrfToken || '',
        },
        credentials: 'include',
      }
    );

    if (!response.ok) {
      throw new Error('Token refresh failed');
    }

    const data = (await response.json()) as RefreshTokenResponse;

    this.scheduleTokenRefresh(29 * 60);

    return {
      success: data.success,
    };
  }

  static async checkAuth(): Promise<CheckAuthResponse> {
    try {
      const response = await fetch(
        `${API_BASE_URL}/account/check-auth`,
        {
          method: 'GET',
          credentials: 'include',
        }
      );

      if (!response.ok) {
        return { isAuthenticated: false };
      }

      const data = (await response.json()) as CheckAuthResponse;
      return data;
    } catch (error) {
      console.error('Auth check failed:', error);
      return { isAuthenticated: false };
    }
  }

  private static scheduleTokenRefresh(expiresIn: number) {
    // Clear existing timeout
    if (this.refreshTimeout) {
      clearTimeout(this.refreshTimeout);
    }

    // Set new timeout to refresh (expiresIn is in seconds)
    const refreshTime = expiresIn * 1000;
    this.refreshTimeout = window.setTimeout(() => {
      this.refreshToken().catch((error) => {
        console.error('Token refresh failed:', error);
      });
    }, refreshTime);
  }

  /**
   * Handle post-login redirection
   * Redirects to the saved URL or falls back to the dashboard
   * @param navigate - The navigate function from React Router
   * @param locationState - The location state from React Router
   */
  static handlePostLoginRedirect(
    navigate: (path: string) => void,
    locationState?: LocationState
  ): void {
    const savedRedirectUrl = RedirectService.getRedirectUrl();

    const locationRedirectUrl = locationState?.from?.pathname;

    const validateRedirectUrl = (
      url: string | null
    ): string | null => {
      if (!url) return null;

      if (url.startsWith('http://') || url.startsWith('https://')) {
        console.warn(
          'External redirect URL blocked for security:',
          url
        );
        return null;
      }

      if (!url.startsWith('/')) {
        return '/' + url;
      }

      return url;
    };

    const validatedSavedUrl = validateRedirectUrl(savedRedirectUrl);
    const validatedLocationUrl = validateRedirectUrl(
      locationRedirectUrl ?? null
    );

    let redirectUrl = '/';

    if (validatedSavedUrl && validatedSavedUrl !== '/login') {
      redirectUrl = validatedSavedUrl;
    } else if (
      validatedLocationUrl &&
      validatedLocationUrl !== '/login'
    ) {
      redirectUrl = validatedLocationUrl;
    } else {
      redirectUrl = '/';
    }

    RedirectService.clearRedirectUrl();

    // Merge guest cart if exists
    const guestCart = localStorage.getItem('guestCart');
    if (guestCart) {
      console.log('Merging guest cart with authenticated cart');
      // We need to trigger cart merge - this should be handled by CartProvider
      // The actual merge will be done when the CartContext initializes
    }

    navigate(redirectUrl);
  }

  static async logout(): Promise<void> {
    if (this.refreshTimeout) {
      clearTimeout(this.refreshTimeout);
      this.refreshTimeout = null;
    }

    try {
      await fetch(`${API_BASE_URL}/account/logout`, {
        method: 'POST',
        credentials: 'include',
      });
    } catch (error) {
      console.error('Logout request failed:', error);
    }
  }
}
