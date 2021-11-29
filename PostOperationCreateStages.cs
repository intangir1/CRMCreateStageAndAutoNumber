using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;

namespace PluginsIncident
{
    public class PostOperationCreateStages : IPlugin
    {
        private ICollection<Entity> GetEntityCollectionByMatch(IOrganizationService organizationService, string entityToFindName, string attributeToCompareName, string attributeValue, ColumnSet columns)
        {
            QueryExpression query = new QueryExpression();
            query.EntityName = entityToFindName;
            query.ColumnSet = columns;
            query.Criteria.AddCondition(new ConditionExpression(attributeToCompareName, ConditionOperator.Equal, attributeValue));
            RetrieveMultipleRequest request = new RetrieveMultipleRequest();
            request.Query = query;
            ICollection<Entity> entities = ((RetrieveMultipleResponse)organizationService.Execute(request)).EntityCollection.Entities;
            return entities;
        }

        private void DeleteOldRelatedStages(IOrganizationService organizationService, EntityReference targetEntityReference)
        {
            Guid idToFind = targetEntityReference.Id;
            string attributeName = "regardingobjectid";
            ColumnSet columnSet = new ColumnSet(new string[] { "mtx_rateint" });

            ICollection<Entity> entities = GetEntityCollectionByMatch(organizationService, mtx_Stage.EntityLogicalName,
                attributeName, idToFind.ToString(), columnSet);
            if(entities.Count <= 0)
            {
                return;
            }

            foreach(Entity entity in entities)
            {
                organizationService.Delete(mtx_Stage.EntityLogicalName, entity.Id);
            }
        }

        private void CreateNewStagesEntities(IOrganizationService organizationService, Incident incident)
        {
            EntityReference stageTemplateEntityReference = incident.mtx_TopicRequiredId;
            if(stageTemplateEntityReference == null)
            {
                return;
            }
            //string attributeName = (new mtx_StageTemplate)).mtx_TopicReadingId.LogicalName;

            string attributeName = "mtx_topicreadingid";
            ColumnSet columnSet = new ColumnSet(new string[] { "mtx_rateint" });
            ICollection<Entity> relatedStageTemplatesEnities = GetEntityCollectionByMatch(organizationService, mtx_StageTemplate.EntityLogicalName,
                attributeName, stageTemplateEntityReference.Id.ToString(), columnSet);
            if (relatedStageTemplatesEnities.Count <= 0)
            {
                return;
            }

            foreach (Entity currentEntity in relatedStageTemplatesEnities)
            {
                InitializeFromRequest initializeFromRequest = new InitializeFromRequest();
                initializeFromRequest.EntityMoniker = new EntityReference(mtx_StageTemplate.EntityLogicalName, currentEntity.Id);
                initializeFromRequest.TargetEntityName = mtx_Stage.EntityLogicalName;
                InitializeFromResponse initializeFromResponse = (InitializeFromResponse)organizationService.Execute(initializeFromRequest);
                Entity childEntity = initializeFromResponse.Entity;
                childEntity["regardingobjectid"] = incident.ToEntityReference();
                childEntity.Id = organizationService.Create(childEntity);
            }
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity && ((Entity)context.InputParameters["Target"]).LogicalName.Equals(Incident.EntityLogicalName))
            {
                Incident incident = ((Entity)context.InputParameters["Target"]).ToEntity<Incident>();
                IOrganizationServiceFactory organizationServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService organizationService = organizationServiceFactory.CreateOrganizationService(context.UserId);

                if(context.MessageName != "Create")
                {
                    DeleteOldRelatedStages(organizationService, incident.ToEntityReference());
                }
                CreateNewStagesEntities(organizationService, incident);
            }
        }
    }
}
