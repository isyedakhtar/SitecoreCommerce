using Sitecore.Commerce.Plugin.Carts;
using System.Collections.Generic;

namespace Habitat.Feature.Carts.Common
{
    public class CartLineUnitListPriceComparer : IComparer<CartLineComponent>
    {
        public int Compare(CartLineComponent x, CartLineComponent y) => x.UnitListPrice.Amount.CompareTo(y.UnitListPrice.Amount);
    }
}
