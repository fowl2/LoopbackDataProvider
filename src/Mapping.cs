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
    sealed class Mapping
    {
        internal static Mapping Create(IOrganizationService service, string primaryEntityName)
        {
            var typeMapFactory = new DefaultTypeMapFactory();
            var queryMapFactory = new QueryMapFactory(service, typeMapFactory);
            return new(queryMapFactory);
        }

        readonly QueryMapFactory queryMapFactory;

        Mapping(QueryMapFactory queryMapFactory)
        {
            this.queryMapFactory = queryMapFactory;
        }

        internal IEnumerable<KeyValuePair<string, object>> ConvertSchema(IEnumerable<KeyValuePair<string, object>> kvps)
            => kvps.Select(ip => new KeyValuePair<string, object>(ip.Key, ConvertSchema(ip.Value)));

        object ConvertSchema(object obj)
            => obj switch
            {
                EntityReference entityReference
                    => ConvertSchema(entityReference),

                Entity entity
                    => ConvertSchema(entity),

                EntityCollection entityCollection
                    => ConvertSchema(entityCollection),

                AttributeCollection attributeCollection
                    => ConvertSchema(attributeCollection),

                QueryExpression queryExpression
                    => ConvertSchema(queryExpression),

                _ => obj,
            };

        EntityCollection ConvertSchema(EntityCollection entityCollection)
        {
            throw new NotImplementedException();
            //var retval = queryExpression.ConvertSchema(queryMap);
           // return retval;
        }

        QueryExpression ConvertSchema(QueryExpression queryExpression)
        {
            var queryMap = queryMapFactory.Create(queryExpression);
            var retval = queryExpression.ConvertSchema(queryMap);
            return retval;
        }

        object ConvertSchema(object obj, string logicalName, string newLogicalName)
            => obj switch
            {
                EntityReference entityReference when entityReference.LogicalName == logicalName
                    => ReplaceEntityLogicalNames(entityReference, logicalName, newLogicalName),

                Entity entity when entity.LogicalName == logicalName
                    => ReplaceEntityLogicalNames(entity, logicalName, newLogicalName),

                EntityCollection entityCollection when entityCollection.EntityName == logicalName
                    => ReplaceEntityLogicalNames(entityCollection, logicalName, newLogicalName),

                AttributeCollection attributeCollection
                    => ReplaceEntityLogicalNames(attributeCollection, logicalName, newLogicalName),

                QueryExpression queryExpression
                    => ReplaceEntityLogicalName(queryExpression, logicalName, newLogicalName),

                _ => obj,
            };



        static QueryExpression ReplaceEntityLogicalName(QueryExpression queryExpression, string logicalName, string newLogicalName)
        {
            var retval = new QueryExpression()
            {
                ColumnSet = queryExpression.ColumnSet,
                Criteria = queryExpression.Criteria,
                EntityName = newLogicalName,
                ExtensionData = queryExpression.ExtensionData,
                NoLock = queryExpression.NoLock,
                PageInfo = queryExpression.PageInfo,
                QueryHints = queryExpression.QueryHints,
                SubQueryExpression = queryExpression.SubQueryExpression,
                Distinct = queryExpression.Distinct,
                TopCount = queryExpression.TopCount,
            };

            retval.LinkEntities.AddRange(queryExpression.LinkEntities);
            retval.Orders.AddRange(queryExpression.Orders);

            return retval;
        }


        public static EntityReference ReplaceEntityLogicalNames(EntityReference entityReference, string logicalName, string newLogicalName)
            => new()
            {
                ExtensionData = entityReference.ExtensionData,
                Id = entityReference.Id,
                KeyAttributes = entityReference.KeyAttributes,
                LogicalName = newLogicalName,
                Name = entityReference.Name,
                RowVersion = entityReference.RowVersion,
            };

        static Entity ReplaceEntityLogicalNames(Entity entityReference, string logicalName, string newLogicalName)
        {
            var retval = new Entity()
            {
                ExtensionData = entityReference.ExtensionData,
                Id = entityReference.Id,
                KeyAttributes = entityReference.KeyAttributes,
                LogicalName = newLogicalName,
                Attributes = ReplaceEntityLogicalNames(entityReference.Attributes, logicalName, newLogicalName),
                RowVersion = entityReference.RowVersion,
                EntityState = entityReference.EntityState,
                HasLazyFileAttribute = entityReference.HasLazyFileAttribute,
                LazyFileAttributeKey = entityReference.LazyFileAttributeKey,
                LazyFileAttributeValue = entityReference.LazyFileAttributeValue,
                LazyFileSizeAttributeKey = entityReference.LazyFileSizeAttributeKey,
                LazyFileSizeAttributeValue = entityReference.LazyFileSizeAttributeValue,
            };

            retval.RelatedEntities.AddRange(ReplaceEntityLogicalNames(retval.RelatedEntities, logicalName, newLogicalName));
            retval.FormattedValues.AddRange(retval.FormattedValues);
            return retval;
        }

        static IEnumerable<Entity> ReplaceEntityLogicalNames(IEnumerable<Entity> entities, string logicalName, string newLogicalName)
            => entities.Select(e => ReplaceEntityLogicalNames(e, logicalName, newLogicalName));

        static IEnumerable<KeyValuePair<Relationship, EntityCollection>> ReplaceEntityLogicalNames(RelatedEntityCollection kvps, string logicalName, string newLogicalName)
            => kvps.Select<KeyValuePair<Relationship, EntityCollection>, KeyValuePair<Relationship, EntityCollection>>(
                _ => throw new NotImplementedException(nameof(RelatedEntityCollection)));

        static AttributeCollection ReplaceEntityLogicalNames(AttributeCollection attributeCollection, string logicalName, string newLogicalName)
        {
            throw new NotImplementedException();
            var retval = new AttributeCollection();
         //   retval.AddRange(
        //        ReplaceEntityLogicalNames(attributeCollection.AsEnumerable(), logicalName, newLogicalName));
            return retval;
        }

        private static EntityCollection ReplaceEntityLogicalNames(EntityCollection entityCollection, string logicalName, string newLogicalName)
        {
            var retval = new EntityCollection()
            {
                EntityName = newLogicalName,
                ExtensionData = entityCollection.ExtensionData,
                MinActiveRowVersion = entityCollection.MinActiveRowVersion,
                MoreRecords = entityCollection.MoreRecords,
                PagingCookie = entityCollection.PagingCookie,
                TotalRecordCount = entityCollection.TotalRecordCount,
                TotalRecordCountLimitExceeded = entityCollection.TotalRecordCountLimitExceeded,
            };

            retval.Entities.AddRange(ReplaceEntityLogicalNames(entityCollection.Entities.AsEnumerable(), logicalName, newLogicalName));
            return retval;
        }
    }
}
