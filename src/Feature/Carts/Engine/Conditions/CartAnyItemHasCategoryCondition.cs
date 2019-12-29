using Habitat.Feature.Carts.Extensions;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Framework.Rules;
using System.Linq;

namespace Habitat.Feature.Carts.Conditions
{
    [EntityIdentifier(CartsConstants.Conditions.CartAnyItemHasCategoryCondition)]
    public class CartAnyItemHasCategoryCondition : ICondition, IMappableRuleEntity, ICartsCondition
    {
        public IRuleValue<string> CategoryId { get; set; }

        public bool Evaluate(IRuleExecutionContext context) => CategoryId.YieldCartLinesWithCategory(context).Any();

    }
}
