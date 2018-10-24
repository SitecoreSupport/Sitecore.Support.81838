using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sitecore.Support.Marketing.xMgmt.Pipelines.DeployDefinition
{
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.Abstractions;
    using Sitecore.Data.Items;
    using Sitecore.Data.Templates;
    using Sitecore.DependencyInjection;
    using Sitecore.Framework.Conditions;
    using Sitecore.Marketing.Core.Extensions;
    using Sitecore.Marketing.Definitions;
    using Sitecore.Marketing.Definitions.AutomationPlans;
    using Sitecore.Marketing.Definitions.AutomationPlans.Model;
    using Sitecore.Marketing.Definitions.Campaigns;
    using Sitecore.Marketing.Definitions.ContactLists;
    using Sitecore.Marketing.Definitions.Events;
    using Sitecore.Marketing.Definitions.Funnels;
    using Sitecore.Marketing.Definitions.Goals;
    using Sitecore.Marketing.Definitions.Outcomes;
    using Sitecore.Marketing.Definitions.Outcomes.Model;
    using Sitecore.Marketing.Definitions.PageEvents;
    using Sitecore.Marketing.Definitions.Profiles;
    using Sitecore.Marketing.Definitions.Segments;
    using Sitecore.Marketing.xMgmt.Extensions;
    using Sitecore.Marketing.xMgmt.Pipelines.DeployDefinition;

    public class Deploy
    {
        private readonly DeploymentManager _deploymentManager;

        private readonly BaseTemplateManager _templateManager;

        public Deploy(DeploymentManager deploymentManager) : this(deploymentManager, ServiceLocator.ServiceProvider.GetRequiredService<BaseTemplateManager>())
        {
        }

        internal Deploy(DeploymentManager deploymentManager, BaseTemplateManager templateManager)
        {
            Condition.Requires<DeploymentManager>(deploymentManager, "deploymentManager").IsNotNull<DeploymentManager>();
            Condition.Requires<BaseTemplateManager>(templateManager, "templateManager").IsNotNull<BaseTemplateManager>();
            this._deploymentManager = deploymentManager;
            this._templateManager = templateManager;
        }

        public virtual void Process(DeployDefinitionArgs args)
        {
            Condition.Requires<DeployDefinitionArgs>(args, "args").IsNotNull<DeployDefinitionArgs>();
            Item item = args.Item;
            Template template = this._templateManager.GetTemplate(item);
            Assembly assembly = Assembly.LoadFile(Sitecore.IO.FileUtil.MapPath("/bin/Sitecore.Marketing.xMgmt.dll"));
            Type type = assembly.GetType("Sitecore.Marketing.xMgmt.Extensions.BaseTemplateManagerExtensions");
            object getTemplatesInheritanceDictionaryMethodInvoke = type.GetMethod("GetTemplatesInheritanceDictionary").Invoke(this, new object[] { this._templateManager, item.Database, Sitecore.Marketing.Definitions.WellKnownIdentifiers.MarketingDefinition.DefinitionTemplateIds });
            Dictionary<Guid, HashSet<Guid>> templatesInheritanceDictionary = getTemplatesInheritanceDictionaryMethodInvoke as Dictionary<Guid, HashSet<Guid>>;
            this.DeployItem<IAutomationPlanDefinition>(item, template, Sitecore.Marketing.Definitions.AutomationPlans.WellKnownIdentifiers.PlanDefinitionTemplateId, templatesInheritanceDictionary);
            this.DeployItem<ICampaignActivityDefinition>(item, template, Sitecore.Marketing.Definitions.Campaigns.WellKnownIdentifiers.CampaignActivityDefinitionTemplateId, templatesInheritanceDictionary);
            this.DeployItem<IEventDefinition>(item, template, Sitecore.Marketing.Definitions.Events.WellKnownIdentifiers.EventDefinitionTemplateId, templatesInheritanceDictionary);
            this.DeployItem<IFunnelDefinition>(item, template, Sitecore.Marketing.Definitions.Funnels.WellKnownIdentifiers.FunnelDefinitionTemplateId, templatesInheritanceDictionary);
            this.DeployItem<IGoalDefinition>(item, template, Sitecore.Marketing.Definitions.Goals.WellKnownIdentifiers.GoalDefinitionTemplateId, templatesInheritanceDictionary);
            this.DeployItem<IOutcomeDefinition>(item, template, Sitecore.Marketing.Definitions.Outcomes.WellKnownIdentifiers.OutcomeDefinitionTemplateId, templatesInheritanceDictionary);
            this.DeployItem<IPageEventDefinition>(item, template, Sitecore.Marketing.Definitions.PageEvents.WellKnownIdentifiers.PageEventDefinitionTemplateId, templatesInheritanceDictionary);
            this.DeployItem<IProfileDefinition>(item, template, Sitecore.Marketing.Definitions.Profiles.WellKnownIdentifiers.ProfileDefinitionTemplateId, templatesInheritanceDictionary);
            this.DeployItem<IContactListDefinition>(item, template, Sitecore.Marketing.Definitions.ContactLists.WellKnownIdentifiers.ContactListDefinitionTemplateId, templatesInheritanceDictionary);
            this.DeployItem<ISegmentDefinition>(item, template, Sitecore.Marketing.Definitions.Segments.WellKnownIdentifiers.SegmentDefinitionTemplateId, templatesInheritanceDictionary);
        }

        protected void DeployItem<TDefinition>(Item item, Template itemTemplate, Guid expectedTemplateId, IReadOnlyDictionary<Guid, HashSet<Guid>> templatesInheritanceDictionary) where TDefinition : IDefinition
        {
            Condition.Requires<Item>(item, "item").IsNotNull<Item>();
            Condition.Requires<Template>(itemTemplate, "itemTemplate").IsNotNull<Template>();
            Condition.Requires<Guid>(expectedTemplateId, "expectedTemplateId").IsNotEmptyGuid();
            if (itemTemplate.InheritsFrom(expectedTemplateId.ToID()))
            {
                HashSet<Guid> source;
                if (!templatesInheritanceDictionary.TryGetValue(expectedTemplateId, out source))
                {
                    throw new InvalidOperationException(string.Format("Unknown definition template id '{0}'", expectedTemplateId));
                }

              #region Sitecore.Support.81838

                //if (!source.Any((Guid c) => itemTemplate.InheritsFrom(c.ToID())) && !this._deploymentManager.DeployAsync<TDefinition>(item.ID.Guid, item.Language.CultureInfo).Wait(TimeSpan.FromSeconds(30.0)))
                //{
                //throw new TimeoutException(string.Format("Save operation for definition id:[{0}] could not be completed within specified timeframe. It will be re-run in the background.", item.ID));
                //}


                if (!source.Any((Guid c) => itemTemplate.InheritsFrom(c.ToID())) && !this._deploymentManager.DeployAsync<TDefinition>(item.ID.Guid, item.Language.CultureInfo).Wait(TimeSpan.Parse(
                   Sitecore.Configuration.Settings.GetSetting("DefinitionDeploy.Timeout", String.Empty))))
                    {
                        throw new TimeoutException(string.Format("Save operation for definition id:[{0}] could not be completed within specified timeframe. It will be re-run in the background.",item.ID));
                    }
              #endregion
            }
        }
    }
}
