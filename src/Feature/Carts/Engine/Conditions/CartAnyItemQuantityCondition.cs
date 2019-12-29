using Habitat.Feature.Carts.Extensions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Framework.Rules;
using System.Linq;

namespace Habitat.Feature.Carts.Conditions
{
    [EntityIdentifier(CartsConstants.Conditions.CartAnyItemQuantityCondition)]
    public class CartAnyItemQuantityCondition : ICartsCondition, ICondition, IMappableRuleEntity
    {
        public IRuleValue<decimal> Quantity { get; set; }

        public IBinaryOperator<decimal, decimal> Operator { get; set; }

        public bool Evaluate(IRuleExecutionContext context)
        {
            var quantity = Quantity.Yield(context);
            Cart cart = context.Fact<CommerceContext>()?.GetObject<Cart>();
            if (cart == null || !cart.Lines.Any() || quantity <= 0 || Operator == null)
            {
                return false;
            }

            var result = Operator.Evaluate(cart.Lines.CountItems(), quantity);

            return result;
        }
    }
}
