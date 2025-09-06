# StyleCommerce

StyleCommerce is a modern, sustainable e-commerce platform built with cutting-edge technologies. The platform focuses on providing an exceptional shopping experience while promoting environmental responsibility through verified sustainable products.

## Key Features

### Core E-commerce Functionality

- **Product Catalog**: Comprehensive product listings with detailed information, images, and sustainability metrics
- **Shopping Cart**: Persistent cart functionality with real-time updates
- **Order Management**: Complete order processing from placement to fulfillment
- **User Authentication**: Secure JWT-based authentication with role-based access control
- **Payment Processing**: Integrated Stripe payment gateway with tokenization for security
- **Inventory Management**: Real-time stock tracking and low inventory alerts

### Sustainability Focus

- **Product Verification**: Automated verification system for sustainable products using AI/ML
- **Eco-Score Rating**: Environmental impact scoring for all products
- **Sustainability Badges**: Visual indicators for verified eco-friendly products
- **Carbon Footprint Tracking**: Order-level carbon footprint calculation

### Advanced Technology Features

- **Augmented Reality (AR)**: Virtual try-on experience for fashion products using Three.js
- **Personalization**: AI-powered product recommendations based on user preferences
- **Analytics Dashboard**: Comprehensive business insights and customer behavior tracking
- **Admin Panel**: Full-featured administration interface for product and order management

### Security & Compliance

- **PCI-DSS Compliance**: Secure payment processing following industry standards
- **Data Protection**: End-to-end encryption for sensitive customer information
- **Audit Logging**: Comprehensive logging of all system activities
- **CORS Protection**: Secure cross-origin resource sharing policies

## Technology Stack

### Backend (C# .NET 9)

- **Framework**: ASP.NET Core 9.0 Web API
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT Bearer Tokens with custom security middleware
- **Payment Processing**: Stripe SDK with tokenization
- **Logging**: Serilog with structured JSON logging
- **Validation**: FluentValidation for request validation
- **Caching**: Redis for performance optimization
- **Background Jobs**: Hangfire for scheduled tasks
- **Security**: OWASP best practices, CSP headers, security middleware

### Frontend (React/TypeScript)

- **Framework**: React 18 with TypeScript
- **Build Tool**: Vite for fast development and production builds
- **UI Library**: Ant Design for responsive components
- **State Management**: React Context API with custom hooks
- **3D/AR**: Three.js for augmented reality product visualization
- **Routing**: React Router v6 for client-side navigation
- **HTTP Client**: Axios for API communication
- **Analytics**: Segment integration for user behavior tracking
- **Testing**: Jest and React Testing Library

### DevOps & Infrastructure

- **Containerization**: Docker and Docker Compose for consistent environments
- **CI/CD**: GitHub Actions for automated testing and deployment
- **Database Migrations**: EF Core migrations for schema management
- **Monitoring**: Structured logging with centralized log management capability
- **Deployment**: Multi-environment configuration (Development, Staging, Production)

## Architecture Overview

The StyleCommerce platform follows a modern microservices-inspired architecture with a clean separation of concerns:

```
┌─────────────────┐    API Calls    ┌──────────────────┐
│   Frontend      │◄────────────────┤   Backend API    │
│  (React/Vite)   │                 │   (ASP.NET 9)    │
└─────────────────┘                 └──────────────────┘
                                             │
                                    ┌──────────────────┐
                                    │   PostgreSQL     │
                                    │   (Database)     │
                                    └──────────────────┘
```

### Backend Architecture

- **Clean Architecture**: Separation of concerns with distinct layers (API, Services, Data)
- **Dependency Injection**: Built-in .NET DI container for loose coupling
- **Repository Pattern**: EF Core repositories for data access abstraction
- **CQRS Pattern**: Command and Query Responsibility Segregation for complex operations
- **Middleware Pipeline**: Custom middleware for security, logging, and error handling

### Frontend Architecture

- **Component-Based**: Reusable UI components with modular CSS
- **Service Layer**: Dedicated service files for API communication
- **Context API**: State management for global application state
- **Custom Hooks**: Reusable logic encapsulation
- **Lazy Loading**: Code splitting for improved performance

## Getting Started

### Prerequisites

- Docker and Docker Compose
- .NET 9.0 SDK (for local development)
- Node.js 22 (for local development)
- Git

### Quick Start with Docker

```bash
# Clone the repository
git clone <repository-url>
cd stylecommerce

# Start all services
docker-compose up --build

# Access the application:
# Frontend: http://localhost:5173
# Backend API: http://localhost:5209
```

### Local Development Setup

1. **Backend Setup**:

   ```bash
   cd backend/api
   dotnet restore
   dotnet run
   ```

2. **Frontend Setup**:
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

## Testing

### Backend Testing

- **Framework**: xUnit with Moq for mocking
- **Coverage**: 80%+ code coverage requirement
- **Types**: Unit tests, integration tests, and controller tests
- **Database**: In-memory database for testing data access

### Frontend Testing

- **Framework**: Jest with React Testing Library
- **Coverage**: 80%+ code coverage requirement
- **Types**: Unit tests for components and services
- **Mocking**: Mock service workers for API call interception

## Security Features

### Authentication & Authorization

- JWT-based authentication with secure token storage
- Role-based access control (Admin, Customer)
- Password hashing with bcrypt
- Session management and token refresh

### Data Protection

- HTTPS enforcement in production
- CORS policy configuration
- Content Security Policy (CSP) headers
- Input validation and sanitization
- SQL injection prevention through EF Core

### Payment Security

- PCI-DSS compliant payment processing
- Stripe tokenization for secure card data handling
- No sensitive payment data stored in application database

### Audit & Compliance

- Comprehensive audit logging for all user actions
- GDPR compliance for user data handling
- Data retention and deletion policies

## API Documentation

The API is documented using OpenAPI/Swagger and can be accessed at:

- Development: `http://localhost:5209/swagger`
- Production: `https://api.stylecommerce.example.com/swagger`

Key API endpoints include:

- **Authentication**: `/api/account/login`, `/api/account/register`
- **Products**: `/api/products`
- **Cart**: `/api/cart`
- **Orders**: `/api/orders`
- **Payments**: `/api/payments`
- **Admin**: `/api/admin` (protected)

## Deployment

### Environment Configuration

The application supports multiple environments:

- **Development**: Local development with Docker Compose
- **Staging**: Pre-production testing environment
- **Production**: Live customer-facing environment

### CI/CD Pipeline

GitHub Actions workflow handles:

- Automated testing on pull requests
- Docker image building and pushing
- Deployment to staging environment
- Production deployment on main branch

### Monitoring & Logging

- Structured JSON logging for easy parsing
- Centralized log management capability
- Health check endpoints for service monitoring
- Performance metrics collection

## Development Guidelines

### Backend Development

- Follow Clean Architecture principles
- Use dependency injection for loose coupling
- Implement proper error handling with global exception middleware
- Write unit tests for all business logic
- Use Entity Framework Core migrations for database changes

### Frontend Development

- Use TypeScript for type safety
- Follow React best practices and hooks patterns
- Implement responsive design with Ant Design components
- Write unit tests for components and services
- Use ESLint and Prettier for code formatting

### Code Quality

- Maintain 80%+ test coverage
- Follow SOLID principles
- Use meaningful variable and function names
- Document complex business logic
- Regular code reviews and pair programming

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support, please open an issue on the GitHub repository or contact the development team.
