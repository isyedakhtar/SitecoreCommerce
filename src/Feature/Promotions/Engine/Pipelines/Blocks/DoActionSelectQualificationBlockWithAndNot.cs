using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Promotions;
using Sitecore.Commerce.Plugin.Rules;
using Sitecore.Framework.Pipelines;
using Sitecore.Framework.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Commerce.Feature.Promotions.Pipelines.Blocks
{
    public class DoActionSelectQualificationBlockWithAndNot : PipelineBlock<EntityView, EntityView, CommercePipelineExecutionContext>
    {
        private readonly IGetConditionsPipeline _getConditionsPipeline;
        private readonly IGetOperatorsPipeline _getOperatorsPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoActionSelectQualificationBlock"/> class.
        /// </summary>
        /// <param name="getConditionsPipeline">The get conditions pipeline.</param>
        /// <param name="getOperatorsPipeline">The get operators pipeline.</param>
        public DoActionSelectQualificationBlockWithAndNot(
            IGetConditionsPipeline getConditionsPipeline,
            IGetOperatorsPipeline getOperatorsPipeline)
        {
            _getConditionsPipeline = getConditionsPipeline;
            _getOperatorsPipeline = getOperatorsPipeline;
        }

        /// <summary>
        /// The run.
        /// </summary>
        /// <param name="arg">
        /// The argument.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The <see cref="EntityView"/>.
        /// </returns>
        public override async Task<EntityView> Run(EntityView arg, CommercePipelineExecutionContext context)
        {
            if (string.IsNullOrEmpty(arg?.Action) || !arg.Action.Equals(context.GetPolicy<KnownPromotionsActionsPolicy>().SelectQualification, StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }

            var promotion = context.CommerceContext.GetObjects<Promotion>().FirstOrDefault(p => p.Id.Equals(arg.EntityId, StringComparison.OrdinalIgnoreCase));
            if (promotion == null)
            {
                return arg;
            }

            var selectedCondition = arg.Properties.FirstOrDefault(p => p.Name.Equals("Condition", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(selectedCondition?.Value))
            {
                var propertyDisplayName = selectedCondition == null ? "Condition" : selectedCondition.DisplayName;
                await context.CommerceContext.AddMessage(
                    context.GetPolicy<KnownResultCodes>().ValidationError,
                    "InvalidOrMissingPropertyValue",
                    new object[] { propertyDisplayName },
                    "Invalid or missing value for property 'Condition'.").ConfigureAwait(false);
                return arg;
            }

            var availableConditions = (await _getConditionsPipeline.Run(typeof(ICondition), context.CommerceContext.PipelineContextOptions).ConfigureAwait(false))?.ToList();
            var condition = availableConditions?.FirstOrDefault(c => c.LibraryId.Equals(selectedCondition.Value, StringComparison.OrdinalIgnoreCase));
            if (condition == null)
            {
                await context.CommerceContext.AddMessage(
                    context.GetPolicy<KnownResultCodes>().ValidationError,
                    "InvalidOrMissingPropertyValue",
                    new object[] { selectedCondition.DisplayName },
                    "Invalid or missing value for property 'Condition'.").ConfigureAwait(false);
                return arg;
            }

            selectedCondition.RawValue = condition.LibraryId;
            selectedCondition.IsReadOnly = true;
            var selectionPolicy = new AvailableSelectionsPolicy(
                availableConditions
                    .Where(s => s.LibraryId.Equals(condition.LibraryId, StringComparison.OrdinalIgnoreCase))
                    .Select(c => new Selection
                    {
                        DisplayName = c.Name,
                        Name = c.LibraryId
                    }).ToList()
            );
            selectedCondition.Policies.Clear();
            selectedCondition.Policies = new List<Policy> { selectionPolicy };
            selectedCondition.IsHidden = !condition.Properties.Any();

            var propertyIndex = arg.Properties.FindIndex(p => p.Name.Equals("Condition", StringComparison.OrdinalIgnoreCase));
            var viewProp = new ViewProperty(new List<Policy>
            {
                new AvailableSelectionsPolicy(new List<Selection>
                {
                    new Selection
                    {
                        DisplayName = "And",
                        Name = "And"
                    },
                    new Selection
                    {
                        DisplayName = "Or",
                        Name = "Or"
                    },
                    new Selection
                    {
                        DisplayName = "AndNot",
                        Name = "AndNot"                        
                    }
                })
            })
            {
                Name = "ConditionOperator",
                RawValue = condition.ConditionOperator ?? string.Empty,
                IsHidden = !promotion.HasPolicy<PromotionQualificationsPolicy>(),
                IsRequired = promotion.HasPolicy<PromotionQualificationsPolicy>()
            };

            arg.Properties.Insert(
                propertyIndex, viewProp);

            foreach (var p in condition.Properties.Where(p => p.IsOperator))
            {
                var viewProperty = new ViewProperty
                {
                    Name = p.Name,
                    RawValue = p.Value ?? string.Empty,
                    OriginalType = p.DisplayType
                };
                var type = string.IsNullOrEmpty(p.DisplayType) ? null : Type.GetType(p.DisplayType);
                var availableOperators = (await _getOperatorsPipeline.Run(type, context.CommerceContext.PipelineContextOptions).ConfigureAwait(false))?.ToList();

                viewProperty.GetPolicy<AvailableSelectionsPolicy>().List.Clear();
                viewProperty.GetPolicy<AvailableSelectionsPolicy>().List.AddRange(availableOperators.Any()
                    ? availableOperators.Select(c => new Selection
                    {
                        DisplayName = c.Name,
                        Name = c.Type
                    }).ToList()
                    : new List<Selection>());

                arg.Properties.Add(viewProperty);
            }

            condition.Properties.Where(p => !p.IsOperator).ForEach(
                p =>
                {
                    var viewProperty = new ViewProperty
                    {
                        Name = p.Name,
                        RawValue = p.Value ?? string.Empty,
                        OriginalType = p.DisplayType
                    };
                    arg.Properties.Add(viewProperty);
                });

            context.CommerceContext.AddModel(new MultiStepActionModel(context.GetPolicy<KnownPromotionsActionsPolicy>().AddQualification));

            return arg;
        }
    }    
}
