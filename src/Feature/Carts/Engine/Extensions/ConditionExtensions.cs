using Habitat.Feature.Carts.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Framework.Rules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Habitat.Feature.Carts.Extensions
{
    public static class ConditionExtensions
    {
        public static IEnumerable<CartLineComponent> YieldCartLinesWithCategory(this IRuleValue<string> ruleValue, IRuleExecutionContext context)
        {
            var targetCategory = ruleValue?.Yield(context);
            var cart = context.Fact<CommerceContext>()?.GetObject<Cart>();

            if (cart == null || !cart.Lines.Any() || string.IsNullOrEmpty(targetCategory))
            {
                return Enumerable.Empty<CartLineComponent>();
            }

            var result = cart.Lines.Where(l => CartLineHasCategory(targetCategory, l));

            return result;
        }

        private static bool CartLineHasCategory(string targetCategory, CartLineComponent line)
        {
            if (!line.HasComponent<CategoryListComponent>())
            {
                return false;
            }

            var targetCategoryList = targetCategory.Split('|');
            var categoryListComponent = line.GetComponent<CategoryListComponent>();
            var result = targetCategoryList.Any(x => categoryListComponent.ParentCategoryList.Contains(x));

            return result;
        }
    }
}
