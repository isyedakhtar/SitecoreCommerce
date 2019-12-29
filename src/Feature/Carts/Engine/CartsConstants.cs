namespace Habitat.Feature.Carts
{
    /// <summary>
    /// At the moment the benifit / qualification terms are hard-coded because we can initialize the terms in Isobar environment
    /// After installing clean commerce envronment swap the commented lines
    /// </summary>
    public static class CartsConstants
    {
        //private const string PREFIX = "BS";
        private const string PREFIX = "BS | ";

        public static class Pipelines
        {
            public static class Blocks
            {
                public const string PopulateCategoryListComponentBlock = "Feature.Carts:Block:" + nameof(PopulateCategoryListComponentBlock);
            }
        }

        public static class Conditions
        {
            //public const string CartAnyItemHasCategoryCondition = PREFIX + nameof(CartAnyItemHasCategoryCondition);
            //public const string CartAnyItemQuantityCondition = PREFIX + nameof(CartAnyItemQuantityCondition);
            public const string CartAnyItemHasCategoryCondition = PREFIX + "Cart Items Exist in [specific] Category?";
            public const string CartAnyItemQuantityCondition = PREFIX + "Cart Items Quantity  [compares] to [specific value]?";
        }

        public static class Actions
        {
            //public const string CartItemXNumberAtYValueAction = PREFIX + nameof(CartItemXNumberAtYValueAction);
            //public const string CartItemXNumberYPercentOffAction = PREFIX + nameof(CartItemXNumberYPercentOffAction);
            //public const string CartItemQuantityMultiplierOfXGetsXAtYPercentOffAction = PREFIX + nameof(CartItemQuantityMultiplierOfXGetsXAtYPercentOffAction);
            //public const string CartItemXQuantityAmountOffAction = PREFIX + nameof(CartItemXQuantityAmountOffAction);
            //public const string CartLineItemInCategoryPercentOffAction = PREFIX + nameof(CartLineItemInCategoryPercentOffAction);
            //public const string CartLineItemInTagPercentOffAction = PREFIX + nameof(CartLineItemInTagPercentOffAction);
            public const string CartItemXNumberAtYValueAction = PREFIX + "Cart Item [specific] number at Price [value].";
            public const string CartItemXNumberYPercentOffAction = PREFIX + "Cart Item [specific] number at [specific] Percent Off.";
            public const string CartItemQuantityMultiplierOfXGetsXAtYPercentOffAction = PREFIX + "Cart Item Quantity Multiplier of [multiplier] Get [specific] Percent Off.";
            public const string CartItemXQuantityAmountOffAction = PREFIX + "Cart Item [specific] Quantity Get [value] Amount Off.";
            public const string CartLineItemInCategoryPercentOffAction = PREFIX + "Get [specific] Percentage Off on Any Cart Line Item in [specific] Category.";
            public const string CartLineItemInTagPercentOffAction = PREFIX + "Get [specific] Percentage Off on Any Cart Line Item in [specific] Tag.";
        }
    }
}
