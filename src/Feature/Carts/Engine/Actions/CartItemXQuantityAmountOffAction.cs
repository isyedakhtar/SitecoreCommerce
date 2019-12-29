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
    /// [AEC-109]
    /// E.g. Buy 2 get $40 back, Buy 4 get $80 back
    /// Qu/Ac: (All Items) / [Buy X get $40 back]
    /// 
    /// Definition: Cart Item [specific] Quantity Get [value] Amount Off.
    /// </summary>
    [EntityIdentifier(CartsConstants.Actions.CartItemXQuantityAmountOffAction)]
    public class CartItemXQuantityAmountOffAction : CartActionBase, ICartAction, ICartsAction, IAction, IMappableRuleEntity
    {
        public IRuleValue<int> Quantity { get; set; }

        public IRuleValue<decimal> AmountOff { get; set; }

        public void Execute(IRuleExecutionContext context)
        {
            var defaultPromotionName = nameof(CartItemXQuantityAmountOffAction);

            var commerceContext = context.Fact<CommerceContext>();
            var cartTotals = commerceContext?.GetObject<CartTotals>();
            var cart = commerceContext?.GetObject<Cart>();
            var propertiesModel = commerceContext.GetObject<PropertiesModel>();

            if (!IsCartEligibleForAction(cart, cartTotals))
            {
                return;
            }

            var eligibleQuantity = Quantity.Yield(context);
            decimal valueOff = AmountOff.Yield(context) * decimal.MinusOne;

            var totalItemsQuantity = cart.Lines.CountItems();

            if (totalItemsQuantity < eligibleQuantity)
            {
                return;
            }

            AddCartAdjustment(cart, valueOff, defaultPromotionName, commerceContext, propertiesModel);

            cartTotals.Cart.SubTotal.Amount = cartTotals.Cart.SubTotal.Amount + valueOff;
            AddCartMessage(cart, defaultPromotionName, commerceContext, propertiesModel);
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
