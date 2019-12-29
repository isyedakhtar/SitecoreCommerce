using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Pricing;
using System;

namespace Habitat.Feature.Carts.Actions
{
    public abstract class CartActionBase
    {
        protected virtual void AddCartLineMessage(CartLineComponent line, string defaultPromotionName, CommerceContext commerceContext, PropertiesModel propertiesModel)
        {
            string message = $"PromotionApplied: {propertiesModel?.GetPropertyValue("PromotionId") ?? defaultPromotionName}";
            line.GetComponent<MessagesComponent>()
                .AddMessage(commerceContext.GetPolicy<KnownMessageCodePolicy>().Promotions, message);
        }

        protected virtual void AddCartMessage(Cart cart, string defaultPromotionName, CommerceContext commerceContext, PropertiesModel propertiesModel)
        {
            string message = $"PromotionApplied: {propertiesModel?.GetPropertyValue("PromotionId") ?? defaultPromotionName}";
            cart.GetComponent<MessagesComponent>()
                .AddMessage(commerceContext.GetPolicy<KnownMessageCodePolicy>().Promotions, message);
        }

        protected virtual decimal CalculateLineDiscountedPercent(decimal percentOff, CommerceContext commerceContext, CartTotals cartTotals, CartLineComponent line)
        {
            decimal total = cartTotals.Lines[line.Id].SubTotal.Amount;
            decimal value = percentOff * total;

            value = RoundValue(commerceContext, value);

            decimal valueOff = value * decimal.MinusOne;

            return valueOff;
        }

        protected virtual decimal CalculateItemDiscountedPercent(decimal percentOff, CommerceContext commerceContext, CartLineComponent line, int count)
        {
            decimal unitPrice = line.UnitListPrice.Amount;
            decimal value = percentOff * unitPrice * count;

            value = RoundValue(commerceContext, value);

            decimal valueOff = value * decimal.MinusOne;

            return valueOff;
        }

        protected virtual decimal CalculateItemDiscountedValue(decimal valuePrice, CommerceContext commerceContext, CartLineComponent line)
        {
            decimal unitPrice = line.UnitListPrice.Amount;
            decimal value = unitPrice - valuePrice;

            value = RoundValue(commerceContext, value);

            decimal valueOff = value * decimal.MinusOne;

            return valueOff;
        }

        protected virtual void AddLineAdjustment(CartLineComponent line, decimal valueOff, string defaultPromotionName, CommerceContext commerceContext, PropertiesModel propertiesModel)
        {
            var discountAdjustmentPolicy = commerceContext.GetPolicy<KnownCartAdjustmentTypesPolicy>().Discount;

            line.Adjustments.Add(new CartLineLevelAwardedAdjustment
            {
                Name = ((propertiesModel?.GetPropertyValue("PromotionText") as string) ?? discountAdjustmentPolicy),
                DisplayName = ((propertiesModel?.GetPropertyValue("PromotionCartText") as string) ?? discountAdjustmentPolicy),
                Adjustment = new Money(commerceContext.CurrentCurrency(), valueOff),
                AdjustmentType = discountAdjustmentPolicy,
                IsTaxable = false,
                AwardingBlock = defaultPromotionName
            });
        }

        protected virtual void AddCartAdjustment(Cart cart, decimal valueOff, string defaultPromotionName, CommerceContext commerceContext, PropertiesModel propertiesModel)
        {
            var discountAdjustmentPolicy = commerceContext.GetPolicy<KnownCartAdjustmentTypesPolicy>().Discount;

            cart.Adjustments.Add(new CartLevelAwardedAdjustment
            {
                Name = ((propertiesModel?.GetPropertyValue("PromotionText") as string) ?? discountAdjustmentPolicy),
                DisplayName = ((propertiesModel?.GetPropertyValue("PromotionCartText") as string) ?? discountAdjustmentPolicy),
                Adjustment = new Money(commerceContext.CurrentCurrency(), valueOff),
                AdjustmentType = discountAdjustmentPolicy,
                IsTaxable = false,
                AwardingBlock = defaultPromotionName
            });
        }

        protected virtual decimal RoundValue(CommerceContext commerceContext, decimal value)
        {
            if (commerceContext.GetPolicy<GlobalPricingPolicy>().ShouldRoundPriceCalc)
            {
                var roundsDigit = commerceContext.GetPolicy<GlobalPricingPolicy>().RoundDigits;
                var midPointRoundUp = commerceContext.GetPolicy<GlobalPricingPolicy>().MidPointRoundUp;
                var roundMode = midPointRoundUp ? MidpointRounding.AwayFromZero : MidpointRounding.ToEven;

                value = Math.Round(value, roundsDigit, roundMode);
            }

            return value;
        }
    }
}
