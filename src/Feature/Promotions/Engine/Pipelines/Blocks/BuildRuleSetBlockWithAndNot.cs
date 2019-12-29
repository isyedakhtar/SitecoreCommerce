using Sitecore.Commerce.Plugin.Rules;
using Sitecore.Framework.Rules;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Framework.Rules.Registry;
using System;
using System.Linq;

namespace Commerce.Feature.Promotions.Pipelines.Blocks
{
    public class BuildRuleSetBlockWithAndNot: BuildRuleSetBlock
    {
        private readonly IEntityRegistry _entityRegistry;
        private IRuleBuilderInit _ruleBuilder = null;
        private readonly IServiceProvider _services;

        public BuildRuleSetBlockWithAndNot(IEntityRegistry entityRegistry, IServiceProvider services): base(entityRegistry, services)
        {
            this._entityRegistry = entityRegistry;
            this._services = services;
            this._ruleBuilder = _services.GetService<IRuleBuilderInit>();
        }

        protected override IRule BuildRule(RuleModel model)
        {
            var firstConditionModel = model.Conditions.First<ConditionModel>(); //The firt conditions with no ConditionalOperator
            var firstConditionMetaData = _entityRegistry.GetMetadata(firstConditionModel.LibraryId);
            IRuleBuilder ruleBuilder = _ruleBuilder.When(firstConditionModel.ConvertToCondition(firstConditionMetaData, this._entityRegistry, this._services));

            for (int index = 1; index < model.Conditions.Count; ++index)
            {
                var conditionModel = model.Conditions[index];
                var conditionMetaData = this._entityRegistry.GetMetadata(conditionModel.LibraryId);
                var condition = conditionModel.ConvertToCondition(conditionMetaData, this._entityRegistry, this._services);

                if (!string.IsNullOrEmpty(conditionModel.ConditionOperator))
                {
                    if (conditionModel.ConditionOperator.ToUpperInvariant() == "OR")
                        ruleBuilder.Or(condition);
                    else if (conditionModel.ConditionOperator.ToUpperInvariant() == "ANDNOT")
                        ruleBuilder.AndNot(condition);
                    else
                        ruleBuilder.And(condition);
                }
            }

            BuildElseAndThen(model, ruleBuilder);
            return ruleBuilder.ToRule();
        }

        private void BuildElseAndThen(RuleModel model, IRuleBuilder ruleBuilder)
        {
            foreach (var thenAction in model.ThenActions)
            {
                var action = thenAction.ConvertToAction(_entityRegistry.GetMetadata(thenAction.LibraryId), this._entityRegistry, this._services);
                ruleBuilder.Then(action);
            }

            foreach (var elseAction in model.ElseActions)
            {
                var action = elseAction.ConvertToAction(_entityRegistry.GetMetadata(elseAction.LibraryId), this._entityRegistry, this._services);
                ruleBuilder.Else(action);
            }
        }
    }
}
