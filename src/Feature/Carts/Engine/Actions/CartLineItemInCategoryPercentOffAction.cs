using Habitat.Feature.Carts.Extensions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Framework.Rules;
using System.Linq;

namespace Habitat.Feature.Carts.Actions
{
    /// <summary>
    /// [AEC-100]
    /// E.g. 10% discount on buying any Ecopia EP100 tyre
    /// Qu/Ac: (All Items) / 10% Off on specific category
    /// 
    /// Definition: Get [specific] Percentage Off on Any Cart Item in [specific] Category.
    /// </summary>
    [EntityIdentifier(CartsConstants.Actions.CartLineItemInCategoryPercentOffAction)]
    public class CartLineItemInCategoryPercentOffAction : CartActionBase, ICartLineAction, ICartsAction, IAction, IMappableRuleEntity
    {
        public IRuleValue<string> CategoryId { get; set; }

        public IRuleValue<decimal> PercentOff { get; set; }

        public void Execute(IRuleExecutionContext context)
        {
            var defaultPromotionName = nameof(CartLineItemInCategoryPercentOffAction);

            var commerceContext = context.Fact<CommerceContext>();
            var cart = commerceContext?.GetObject<Cart>();
            var cartTotals = commerceContext?.GetObject<CartTotals>();

            var categoryId = CategoryId.Yield(context);

            if (!IsCartEligibleForAction(cart, cartTotals, categoryId))
            {
                return;
            }

            var lines = cart.Lines.WhereInCategroy(categoryId);

            if (!lines.Any())
            {
                return;
            }

            var propertiesModel = commerceContext.GetObject<PropertiesModel>();
            decimal percentOff = PercentOff.Yield(context) * 0.01m;

            foreach (var line in lines)
            {
                decimal valueOff = CalculateLineDiscountedPercent(percentOff, commerceContext, cartTotals, line);

                AddLineAdjustment(line, valueOff, defaultPromotionName, commerceContext, propertiesModel);

                cartTotals.Lines[line.Id].SubTotal.Amount = cartTotals.Lines[line.Id].SubTotal.Amount + valueOff;
                AddCartLineMessage(line, defaultPromotionName, commerceContext, propertiesModel);
            }
        }
        
        private bool IsCartEligibleForAction(Cart cart, CartTotals cartTotals, string categoryId)
        {
            return cart != null && 
                   cart.Lines.Any() &&
                   !string.IsNullOrWhiteSpace(categoryId) &&
                   cartTotals != null && 
                   cartTotals.Lines.Any();
        }
    }
}
