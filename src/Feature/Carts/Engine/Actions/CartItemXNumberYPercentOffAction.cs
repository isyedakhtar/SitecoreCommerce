using Habitat.Feature.Carts.Extensions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Framework.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Habitat.Feature.Carts.Actions
{
    /// <summary>
    /// [AEC-99]
    /// E.g. 4th tyre free, 4th tyre for $10
    /// Qu/Ac: (All Items) / [X item free* (100% off) | X item for $10]
    /// 
    /// Definition: Cart Item [specific] number at [specific] Percent Off.
    /// </summary>
    [EntityIdentifier(CartsConstants.Actions.CartItemXNumberYPercentOffAction)]
    public class CartItemXNumberYPercentOffAction : CartActionBase, ICartLineAction, ICartsAction, IAction, IMappableRuleEntity
    {
        public IRuleValue<int> Number { get; set; }

        public IRuleValue<decimal> PercentOff { get; set; }

        public void Execute(IRuleExecutionContext context)
        {
            var defaultPromotionName = nameof(CartItemXNumberYPercentOffAction);

            var commerceContext = context.Fact<CommerceContext>();
            var cartTotals = commerceContext?.GetObject<CartTotals>();
            var cart = commerceContext?.GetObject<Cart>();
            var propertiesModel = commerceContext.GetObject<PropertiesModel>();

            if (!IsCartEligibleForAction(cart, cartTotals))
            {
                return;
            }

            var number = Number.Yield(context);
            decimal percentOff = PercentOff.Yield(context) * 0.01m;

            var totalItemsQuantity = cart.Lines.CountItems();

            if (totalItemsQuantity < number)
            {
                return;
            }

            var cheapestLine = cart.Lines.WhereCheapest().FirstOrDefault();

            if (cheapestLine == null)
            {
                return;
            }

            decimal valueOff = 0m;

            valueOff = CalculateItemDiscountedPercent(percentOff, commerceContext, cheapestLine, 1);

            AddLineAdjustment(cheapestLine, valueOff, defaultPromotionName, commerceContext, propertiesModel);

            cartTotals.Lines[cheapestLine.Id].SubTotal.Amount = cartTotals.Lines[cheapestLine.Id].SubTotal.Amount + valueOff;
            AddCartLineMessage(cheapestLine, defaultPromotionName, commerceContext, propertiesModel);
        }

        private bool IsCartEligibleForAction(Cart cart, CartTotals cartTotals)
        {
            return cart != null &&
                   cart.Lines.Any() &&
                   cartTotals != null &&
                   cartTotals.Lines.Any();
        }
    }
}
