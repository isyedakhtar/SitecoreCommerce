using Habitat.Feature.Carts.Extensions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Framework.Rules;
using System;
using System.Linq;

namespace Habitat.Feature.Carts.Actions
{
    /// <summary>
    /// [AEC-11]
    /// E.g. Offer 1 tyre at half price when buying multipliers of 2.
    /// Multiplier 3, cart item 6, [6 / 3] = 2 tyres off
    /// Multiplier 2, cart item 6, [6 / 2] = 3 tyres off
    /// Qu/Ac: (All Items) / [2X get X 50% off]
    /// 
    /// Definition: Cart Item Quantity Multiplier of [multiplier] Get [specific] Percent Off.
    /// </summary>
    [EntityIdentifier(CartsConstants.Actions.CartItemQuantityMultiplierOfXGetsXAtYPercentOffAction)]
    public class CartItemQuantityMultiplierOfXGetsXAtYPercentOffAction : CartActionBase, ICartLineAction, ICartsAction, IAction, IMappableRuleEntity
    {
        public IRuleValue<int> Multiplier { get; set; }
        public IRuleValue<decimal> PercentOff { get; set; }

        public void Execute(IRuleExecutionContext context)
        {
            var defaultPromotionName = nameof(CartItemQuantityMultiplierOfXGetsXAtYPercentOffAction);
            var commerceContext = context.Fact<CommerceContext>();
            var cart = commerceContext?.GetObject<Cart>();
            var cartTotals = commerceContext?.GetObject<CartTotals>();
            var propertiesModel = commerceContext.GetObject<PropertiesModel>();

            var multiplier = Multiplier.Yield(context);

            if (!IsCartEligibleForAction(cart, cartTotals, multiplier))
            {
                return;
            }

            var totalItemsQuantity = cart.Lines.CountItems();
            var discountedItemsCount = (int)Math.Floor((decimal)totalItemsQuantity / multiplier);

            if (discountedItemsCount < 1)
            {
                return;
            }

            var eligibleLines = cart.Lines.WhereCheapest(discountedItemsCount);
            decimal percentOff = PercentOff.Yield(context) * 0.01m;

            foreach (var line in eligibleLines)
            {
                decimal valueOff = 0m;

                if (line.Quantity > discountedItemsCount)
                {
                    valueOff = CalculateItemDiscountedPercent(percentOff, commerceContext, line, discountedItemsCount);
                }
                else
                {
                    valueOff = CalculateLineDiscountedPercent(percentOff, commerceContext, cartTotals, line);
                    discountedItemsCount -= (int)line.Quantity;
                }

                AddLineAdjustment(line, valueOff, defaultPromotionName, commerceContext, propertiesModel);

                cartTotals.Lines[line.Id].SubTotal.Amount = cartTotals.Lines[line.Id].SubTotal.Amount + valueOff;
                AddCartLineMessage(line, defaultPromotionName, commerceContext, propertiesModel);
            }
        }

        private bool IsCartEligibleForAction(Cart cart, CartTotals cartTotals, int multiplier)
        {
            return cart != null &&
                   cart.Lines.Any() &&
                   multiplier > 0 &&
                   cartTotals != null &&
                   cartTotals.Lines.Any();
        }
    }
}
