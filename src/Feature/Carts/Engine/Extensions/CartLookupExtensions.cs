using Habitat.Feature.Carts.Common;
using Habitat.Feature.Carts.Components;
using Sitecore.Commerce.Plugin.Carts;
using System.Collections.Generic;
using System.Linq;

namespace Habitat.Feature.Carts.Extensions
{
    public static class CartLookupExtensions
    {
        public static IEnumerable<CartLineComponent> WhereInCategroy(this IEnumerable<CartLineComponent> lines, string categoryId)
        {
            var result = lines.Where(l => l.HasComponent<CategoryListComponent>() &&
                                          l.GetComponent<CategoryListComponent>()
                                           .ParentCategoryList
                                           .Contains(categoryId));

            return result;
        }

        public static IEnumerable<CartLineComponent> WhereInTag(this IEnumerable<CartLineComponent> lines, string tag)
        {
            var result = lines.Where(l => l.HasComponent<CartProductComponent>() &&
                                          l.GetComponent<CartProductComponent>()
                                           .Tags
                                           .Any(t => t.Name.Equals(tag, System.StringComparison.OrdinalIgnoreCase)));

            return result;
        }

        public static IEnumerable<CartLineComponent> WhereCheapest(this IEnumerable<CartLineComponent> lines, int take = 1)
        {
            var orderedLines = lines.OrderBy(l => l, new CartLineUnitListPriceComparer());
            decimal iterator = 0m;

            foreach (var line in orderedLines)
            {
                yield return line;
                iterator += line.Quantity;

                if (iterator >= take)
                {
                    break;
                }
            }
        }

        public static int CountItems(this IEnumerable<CartLineComponent> lines)
        {
            return (int)lines.Sum(l => l.Quantity);
        }

    }
}
