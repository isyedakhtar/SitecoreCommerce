using Habitat.Feature.Carts.Components;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.SQL;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Habitat.Feature.Carts.Pipelines.Blocks
{
    [PipelineDisplayName(CartsConstants.Pipelines.Blocks.PopulateCategoryListComponentBlock)]

    public class PopulateCategoryListComponentBlock : PipelineBlock<CartLineComponent, CartLineComponent, CommercePipelineExecutionContext>
    {
        private readonly CommerceCommander _commerceCommander;

        public PopulateCategoryListComponentBlock(CommerceCommander commerceCommander)
        {
            _commerceCommander = commerceCommander;
        }

        public override async Task<CartLineComponent> Run(CartLineComponent arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The CartLineComponent can not be null");

            if (arg.HasComponent<CategoryListComponent>())
                return arg;

            var productArgument = ProductArgument.FromItemId(arg.ItemId);
            if (!productArgument.IsValid())
            {
                return arg;
            }

            var sellableItem = context.CommerceContext.GetEntity<SellableItem>(s => s.ProductId.Equals(productArgument.ProductId, StringComparison.OrdinalIgnoreCase));
            if (sellableItem == null)
            {
                return arg;
            }

            var component = arg.GetComponent<CategoryListComponent>();
            component.Id = sellableItem.FriendlyId;
            component.ParentCategoryList = await GetCategoryName(sellableItem.ParentCategoryList, context);

            return arg;
        }

        private async Task<string> GetCategoryName(string categoryList, CommercePipelineExecutionContext context)
        {
            var categoryIds = new List<string>();
            var categories = categoryList.Split('|').ToList();
            var entityIds = await _commerceCommander.Command<GetEntityIdsForSitecoreIdsCommand>().Process(context.CommerceContext, categories);

            foreach (var item in entityIds)
            {
                var category = await _commerceCommander.Command<GetCategoryCommand>().Process(context.CommerceContext, item);
                if (category != null)
                {
                    categoryIds.Add(category.Id);
                }
            }

            return categoryIds.Any() ?
                string.Join("|", categoryIds) : string.Empty;
        }
    }
}
