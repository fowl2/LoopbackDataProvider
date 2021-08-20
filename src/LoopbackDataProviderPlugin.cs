
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Data.Exceptions;
using Microsoft.Xrm.Sdk.Data.Extensions;
using Microsoft.Xrm.Sdk.Data.Mappings;
using Microsoft.Xrm.Sdk.Extensions;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;


using System;
using System.Collections.Generic;
using System.Linq;

namespace LoopbackDataProvider
{
    public sealed class LoopbackDataProviderPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.Get<IPluginExecutionContext>()
                ?? throw new ArgumentException($"Get<{nameof(IPluginExecutionContext)}>() returned null");
            var service = serviceProvider.GetOrganizationService(Guid.Empty)
                ?? throw new ArgumentException($"GetOrganizationService() returned null");
            var tracing = serviceProvider.Get<ITracingService>()
                ?? throw new ArgumentException($"Get<{nameof(ITracingService)}>() returned null");

            tracing.Trace($"Rev: {GitVersionInformation.InformationalVersion}");

            foreach (var item in context.InputParameters)
                tracing.Trace($"IP: {item.Key}: {item.Value} ({item.Value?.GetType()})");

            foreach (var item in context.OutputParameters)
                tracing.Trace($"OP: {item.Key}: {item.Value} ({item.Value?.GetType()})");

            foreach (var item in context.SharedVariables)
                tracing.Trace($"SV: {item.Key}: {item.Value} ({item.Value?.GetType()})");


            var retriever = serviceProvider.Get<IEntityDataSourceRetrieverService>()
                   ?? throw new ArgumentException($"Get<{nameof(IEntityDataSourceRetrieverService)}>() returned null");
            var dataSource = retriever.RetrieveEntityDataSource()
                ?? throw new ArgumentException("RetrieveEntityDataSource() returned null");

            var targetEntityName = dataSource.Attributes.First(x => x.Value is string).Value.ToString();


            var mapping = Mapping.Create(service, context.PrimaryEntityName);

            var entityMetadata = service.GetEntityMetadata(context.PrimaryEntityName);

            var typeMapFactory = new DefaultTypeMapFactory();


            switch (context.MessageName)
            {
                case "Retrieve":
                    var entityMap =EntityMapFactory.Create(entityMetadata, typeMapFactory, "mappingEntityAlias");
                    
                    var target = (EntityReference)context.InputParameters["Target"];
                    var columnSet = (ColumnSet)context.InputParameters["ColumnSet"];
                    var relatedEntitiesQuery = (RelationshipQueryCollection)context.InputParameters["RelatedEntitiesQuery"];
                    if (relatedEntitiesQuery.Any())
                        throw new NotImplementedException("RelatedEntitiesQuery");
                    
                    var mappedTarget = Mapping.ReplaceEntityLogicalNames(target, context.PrimaryEntityName, entityMetadata.ExternalName);
                    var mappedColumns = columnSet.Columns.Select(entityMap.MapAttributeNameExternal).ToArray();

                    var retrieveResponse = (RetrieveResponse)service.Execute(new RetrieveRequest { 
                        
                        ColumnSet = new (mappedColumns),
                        Target = mappedTarget,
                    });

                    var mappedEntity = retrieveResponse.Entity.ToEntity(entityMap);
                    context.OutputParameters["Entity"] = mappedEntity;
                    return;
                    
                case "RetrieveMultiple":
                    
                    var queryMapFactory = new QueryMapFactory(service, typeMapFactory);

                    var query = (QueryExpression)context.InputParameters["Query"];
                    var queryMap = queryMapFactory.Create(query);
                    
                    var convertedQuery = query.ConvertSchema(queryMap);

                    tracing.Trace($"{nameof(convertedQuery)}: {convertedQuery.EntityName} ({string.Join(", ", convertedQuery.ColumnSet.Columns)})");
                    tracing.Trace($"{nameof(convertedQuery)}: Conditions: {convertedQuery.Criteria.Conditions.Count()}");

                    var retrieveMultipleResponse = service.RetrieveMultiple(convertedQuery);
                    var mappedEntities = new EntityCollection();
                     
                    mappedEntities.Entities.AddRange(retrieveMultipleResponse.Entities.Select(x=> x.ToEntity(queryMap.PrimaryEntityMap)));

                    foreach (var item in mappedEntities.Entities)
                    {
                        tracing.Trace($"{item.LogicalName} {item.Id} ({string.Join(",", item.Attributes.Select(a=> $"{a.Key}: {a.Value} ({a.Value?.GetType()?.Name})"))})");
                    }

                    mappedEntities.MoreRecords = retrieveMultipleResponse.MoreRecords;
                    mappedEntities.MinActiveRowVersion = retrieveMultipleResponse.MinActiveRowVersion;
                    mappedEntities.TotalRecordCountLimitExceeded = retrieveMultipleResponse.TotalRecordCountLimitExceeded;
                    mappedEntities.TotalRecordCount = retrieveMultipleResponse.TotalRecordCount;
                    mappedEntities.PagingCookie = retrieveMultipleResponse.PagingCookie;

                    context.OutputParameters["BusinessEntityCollection"] = mappedEntities; 
                    return;

                case "Create":
                case "Update":
                case "Delete":
                default:
                    break;
            }

            var request = new OrganizationRequest(context.MessageName);

            request.Parameters.AddRange(
                mapping.ConvertSchema(context.InputParameters));

            var response = service.Execute(request);

            context.OutputParameters.AddRange(
                mapping.ConvertSchema(response.Results));
        }


    }
}