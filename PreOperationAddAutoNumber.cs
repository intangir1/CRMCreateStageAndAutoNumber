using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginIncident
{
    public class PreOperationAddAutoNumber : IPlugin
    {
        private Entity FindRelatedObjectiveNumber(IOrganizationService organizationService)
        {
            const int objectiveValue = 1;

            ConditionExpression condition1 = new ConditionExpression();
            condition1.AttributeName = "mtx_objectivecode";
            condition1.Operator = ConditionOperator.Equal;
            condition1.Values.Add(objectiveValue);

            FilterExpression filter1 = new FilterExpression();
            filter1.Conditions.Add(condition1);

            QueryExpression query = new QueryExpression(mtx_IncrementNumber.EntityLogicalName);
            query.ColumnSet.AddColumns("mtx_objectivecode", "mtx_incrementingnumberint");
            query.Criteria.AddFilter(filter1);

            EntityCollection collection = organizationService.RetrieveMultiple(query);
            if(collection.Entities.Count == 0)
            {
                return null;
            }
            return collection.Entities[0];
        }

        public void Execute(IServiceProvider serviceProvider)
        {
			IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

			if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity && ((Entity)context.InputParameters["Target"]).LogicalName.Equals(Incident.EntityLogicalName))
			{
				Incident incident = ((Entity)context.InputParameters["Target"]).ToEntity<Incident>();

                IOrganizationServiceFactory organizationServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService organizationService = organizationServiceFactory.CreateOrganizationService(context.UserId);
                
                Entity relatedObjectiveNumber = FindRelatedObjectiveNumber(organizationService);
                if (relatedObjectiveNumber == null)
                {
                    throw new InvalidPluginExecutionException("Related objective increment number not found!");
                }

                int incidentNumber = (int)relatedObjectiveNumber["mtx_incrementingnumberint"];
                incident.mtx_RequestNumberInt = incidentNumber;

                Entity updateEntity = new Entity(mtx_IncrementNumber.EntityLogicalName);
                updateEntity.Id = relatedObjectiveNumber.Id;
                organizationService.Update(updateEntity);
                updateEntity["mtx_incrementingnumberint"] = (int)relatedObjectiveNumber["mtx_incrementingnumberint"] + 1;
                organizationService.Update(updateEntity);
            }
		}
    }
}
