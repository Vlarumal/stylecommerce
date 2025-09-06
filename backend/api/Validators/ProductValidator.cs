using FluentValidation;
using StyleCommerce.Api.Models;

namespace StyleCommerce.Api.Validators
{
    public class ProductValidator : AbstractValidator<Product>
    {
        public ProductValidator()
        {
            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage("Product name is required")
                .MaximumLength(100)
                .WithMessage("Product name must not exceed 100 characters");

            RuleFor(p => p.Description)
                .MaximumLength(500)
                .WithMessage("Product description must not exceed 500 characters");

            RuleFor(p => p.Price)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Price must be greater than or equal to 0");

            RuleFor(p => p.Brand)
                .MaximumLength(50)
                .WithMessage("Brand must not exceed 50 characters");

            RuleFor(p => p.Size)
                .MaximumLength(20)
                .WithMessage("Size must not exceed 20 characters");

            RuleFor(p => p.Color)
                .MaximumLength(30)
                .WithMessage("Color must not exceed 30 characters");

            RuleFor(p => p.StockQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Stock quantity must be greater than or equal to 0");

            RuleFor(p => p.VerificationScore)
                .InclusiveBetween(0, 100)
                .WithMessage("VerificationScore must be between 0 and 100");

            RuleFor(p => p.ImageUrl)
                .MaximumLength(200)
                .WithMessage("ImageUrl must not exceed 200 characters");

            RuleFor(p => p.EcoScore)
                .InclusiveBetween(0, 100)
                .WithMessage("EcoScore must be between 0 and 100");
        }
    }
}
