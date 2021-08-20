using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;
using Microsoft.Xrm.Sdk.Data.Mappings;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Data.Extensions;
using Microsoft.Xrm.Sdk.Messages;

namespace LoopbackDataProvider
{
    static class ConversionExtensions
    {
        public static QueryExpression ConvertSchema(QueryExpression queryExpression, QueryMapFactory queryMapFactory)
        {
            var queryMap = queryMapFactory.Create(queryExpression);
            var retval = queryExpression.ConvertSchema(queryMap);
            return retval;
        }

        public static ColumnSet ConvertSchema(ColumnSet columnSet, EntityMap entityMap)
            => new(columnSet.Columns.Select(entityMap.MapAttributeNameExternal).ToArray());

        public static EntityReference ConvertSchema(EntityReference entityReference, EntityMap entityMap)
            => new()
            {
                LogicalName = entityMap.NameMap.ExternalName,
                Id = entityReference.Id,
                KeyAttributes = ConvertSchema(entityReference.KeyAttributes, entityMap),

                Name = entityReference.Name,
                ExtensionData = entityReference.ExtensionData,
                RowVersion = entityReference.RowVersion,
            };

        public static KeyAttributeCollection ConvertSchema(KeyAttributeCollection keyAttributeCollection, EntityMap entityMap)
        {
            var result = new KeyAttributeCollection();
            result.AddRange(keyAttributeCollection
                .Select(attr => new KeyValuePair<string, object>(
                    entityMap.MapAttributeNameExternal(attr.Key), attr.Value)
                ));
            return result;
        }

        public static EntityCollection ConvertSchema(EntityCollection externalEntites, QueryMap queryMap)
        {
            var mappedEntities = new EntityCollection()
            {
                EntityName = queryMap.PrimaryEntityMap.NameMap.XrmName,
            };
            mappedEntities.MoreRecords = externalEntites.MoreRecords;
            mappedEntities.MinActiveRowVersion = externalEntites.MinActiveRowVersion;
            mappedEntities.TotalRecordCountLimitExceeded = externalEntites.TotalRecordCountLimitExceeded;
            mappedEntities.TotalRecordCount = externalEntites.TotalRecordCount;
            mappedEntities.PagingCookie = externalEntites.PagingCookie;
            mappedEntities.Entities.AddRange(externalEntites.Entities.Select(x => ConvertSchema(x, queryMap)));

            return mappedEntities;
        }

        public static Entity ConvertSchema(Entity externalEntity, QueryMap queryMap)
        {
            // TODO: Link entities
            return ConvertSchema(externalEntity, queryMap.PrimaryEntityMap);
        }

        public static Entity ConvertSchema(Entity externalEntity, EntityMap entityMap)
        {
            Entity convertedEntity = new(entityMap.NameMap.XrmName);

            foreach (AttributeMap attribute in entityMap.AttributeMap)
            {
                if (!externalEntity.Attributes.TryGetValue(attribute.NameMap.ExternalName, out var externalValue))
                    continue;

                // TODO: value conversion?
                convertedEntity.Attributes[attribute.NameMap.XrmName] = externalValue;

                if (attribute.IsPrimaryAttributeId)
                {
                    convertedEntity.Id = (Guid)externalValue;
                }
            }

            return convertedEntity;
        }

        public static Entity ConvertSchemaExternal(Entity xrmEntity, EntityMap entityMap)
        {
            Entity convertedEntity = new(entityMap.NameMap.ExternalName, xrmEntity.Id);

            foreach (AttributeMap attribute in entityMap.AttributeMap)
            {
                if (!xrmEntity.Attributes.TryGetValue(attribute.NameMap.XrmName, out var xrmValue))
                    continue;

                // TODO: value conversion?
                convertedEntity.Attributes[attribute.NameMap.ExternalName] = xrmValue;
            }

            return convertedEntity;
        }
    }
}
