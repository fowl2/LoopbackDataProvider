
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
        private const string RelatedEntitiesQuery = "RelatedEntitiesQuery";
        private const string Query = "Query";
        private const string Target = "Target";
        private const string ColumnSet = "ColumnSet";
        private const string Entity = "Entity";
        private const string BusinessEntityCollection = "BusinessEntityCollection";

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

            var entityMetadata = service.GetEntityMetadata(context.PrimaryEntityName);
            var typeMapFactory = new DefaultTypeMapFactory();

            switch (context.MessageName)
            {
                case "Retrieve":
                    var entityMap = EntityMapFactory.Create(entityMetadata, typeMapFactory, entityAlias: null);

                    var target = (EntityReference)context.InputParameters[Target];
                    var columnSet = (ColumnSet)context.InputParameters[ColumnSet];
                    var relatedEntitiesQuery = (RelationshipQueryCollection)context.InputParameters[RelatedEntitiesQuery];
                    if (relatedEntitiesQuery.Any())
                        throw new NotImplementedException(RelatedEntitiesQuery);
                    
                    var retrieveResponse = (RetrieveResponse)service.Execute(new RetrieveRequest
                    {
                        ColumnSet = ConversionExtensions.ConvertSchema(columnSet, entityMap),
                        Target = ConversionExtensions.ConvertSchema(target, entityMap),
                    });

                    var mappedEntity = ConversionExtensions.ConvertSchema(retrieveResponse.Entity, entityMap);
                    context.OutputParameters[Entity] = mappedEntity;
                    return;

                case "RetrieveMultiple":
                    var queryMapFactory = new QueryMapFactory(service, typeMapFactory);

                    var query = (QueryExpression)context.InputParameters[Query];
                    var queryMap = queryMapFactory.Create(query);

                    foreach (var attr in queryMap.PrimaryEntityMap.AttributeMap)
                        tracing.Trace($"attr ({attr.IsPrimaryAttributeId}) : {attr.NameMap.ExternalName} -> {attr.NameMap.XrmName}");

                    var convertedQuery = query.ConvertSchema(queryMap);

                    tracing.Trace($"{nameof(convertedQuery)}: {convertedQuery.EntityName} ({string.Join(", ", convertedQuery.ColumnSet.Columns)})");
                    tracing.Trace($"{nameof(convertedQuery)}: Conditions: {convertedQuery.Criteria.Conditions.Count()}");

                    var retrieveMultipleResponse = service.RetrieveMultiple(convertedQuery);

                    var mappedEntities = ConversionExtensions.ConvertSchema(retrieveMultipleResponse, queryMap);

                    foreach (var item in mappedEntities.Entities)
                        tracing.Trace($"{item.LogicalName} {item.Id} ({string.Join(",", item.Attributes.Select(a => $"{a.Key}: {a.Value} ({a.Value?.GetType()?.Name})"))})");

                    context.OutputParameters[BusinessEntityCollection] = mappedEntities;
                    return;

                case "Create":
                case "Update":
                case "Delete":
                default:
                    throw new NotImplementedException($"Message '{context.MessageName}'");
            }
        }
    }
}