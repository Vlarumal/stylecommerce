import { ThemeConfig } from 'antd';

// Export the full ThemeConfig type for components that need the complete theme object
export type AppTheme = ThemeConfig;

// Export a simplified theme type for components that only use specific properties
export interface SimpleTheme {
  token: {
    colorPrimary?: string;
    colorError?: string;
  };
}