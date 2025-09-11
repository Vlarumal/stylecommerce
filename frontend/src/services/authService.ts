const API_BASE_URL = `${import.meta.env.VITE_BASE_URL}/api`;
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
}

export interface RegisterRequest {
  username: string;
  password: string;
  email: string;
  firstName?: string;
  lastName?: string;
}

export interface RegisterResponse {
  success: boolean;
  message: string;
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
  ): Promise<LoginResponse & { error?: string }> {
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
      if (response.status === 401) {
        try {
          const errorText = await response.text();
          let errorMessage = 'Invalid username or password';
          
          if (errorText) {
            try {
              const errorData: unknown = JSON.parse(errorText);
              errorMessage = typeof errorData === 'string'
                ? errorData
                : (errorData as { message?: string }).message || errorMessage;
            } catch {
              errorMessage = errorText;
            }
          }
          
          throw new Error(errorMessage);
        } catch {
          throw new Error('Invalid username or password');
        }
      }
      throw new Error('Login failed');
    }

    const data = (await response.json()) as LoginResponse;

    // Schedule token refresh (30 minutes - 1 minute)
    this.scheduleTokenRefresh(29 * 60);

    return {
      success: data.success,
    };
  }

  static async register(
    credentials: RegisterRequest
  ): Promise<RegisterResponse> {
    const csrfToken = getCsrfToken();
    const response = await fetch(`${API_BASE_URL}/account/register`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-XSRF-TOKEN': csrfToken || '',
      },
      body: JSON.stringify(credentials),
      credentials: 'include',
    });

    if (!response.ok) {
      try {
        const errorText = await response.text();
        let errorMessage = 'Registration failed';
        
        if (errorText) {
          try {
            const errorData: unknown = JSON.parse(errorText);
            errorMessage = typeof errorData === 'string'
              ? errorData
              : (errorData as { message?: string }).message || errorMessage;
          } catch {
            errorMessage = errorText;
          }
        }
        
        return {
          success: false,
          message: errorMessage
        };
      } catch {
        return {
          success: false,
          message: 'Registration failed'
        };
      }
    }

    const data = (await response.json()) as RegisterResponse;
    return data;
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
   * Redirects to the original page or falls back to the dashboard
   * @param navigate - The navigate function from React Router
   * @param locationState - The location state from React Router
   */
  static handlePostLoginRedirect(
    navigate: (path: string, options?: { replace: boolean }) => void,
    locationState?: {
      from?: {
        pathname: string;
        search?: string;
      }
    }
  ): void {
    let redirectUrl = '/';
    
    // Safe path patterns (extend as needed)
    const safePaths = [
      '/',
      '/products',
      '/categories',
      '/profile',
      '/cart',
      '/checkout',
      '/order-confirmation',
      '/order-details',
      '/order-history',
      '/admin',
      '/product'
    ];
    
    if (locationState?.from) {
      const { pathname, search = '' } = locationState.from;
      const fullPath = `${pathname}${search}`;
      
      const isValidPath = safePaths.some(safePath =>
        pathname.startsWith(safePath) || pathname === '/'
      );
      
      if (isValidPath) {
        redirectUrl = fullPath;
      }
    }

    // Navigate with replace:true to prevent back navigation to login
    navigate(redirectUrl, { replace: true });
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
