using Habitat.Feature.Carts.Pipelines.Blocks;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Configuration;
using Sitecore.Framework.Pipelines.Definitions.Extensions;
using Sitecore.Framework.Rules;
using System.Reflection;

namespace Habitat.Feature.Carts
{
    /// <summary>
    /// The configure sitecore class.
    /// </summary>
    public class ConfigureSitecore : IConfigureSitecore
    {
        /// <summary>
        /// The configure services.
        /// </summary>
        /// <param name="services">
        /// The services.
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.Sitecore().Rules(rule => rule.Registry(reg => reg.RegisterAssembly(assembly)));

            services.Sitecore().Pipelines(config => config
                .ConfigurePipeline<IPopulateLineItemPipeline>(pipeline => 
                {
                    pipeline.Add<PopulateCategoryListComponentBlock>().After<PopulateLineItemProductBlock>();
                })
            );
        }
    }
}