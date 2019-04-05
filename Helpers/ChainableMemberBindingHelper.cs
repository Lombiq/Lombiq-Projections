using Lombiq.Projections.Extensions;
using Orchard.ContentManagement;
using Orchard.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Projections.Helpers
{
    public static class ChainableMemberBindingHelper
    {
        public static IEnumerable<string> GetRecordListReferencePropertyNames(Type bindingRecordType, string propertyPath)
        {
            // The starting point of the query needs to be a ContentPart(Version)Record.
            if (!bindingRecordType.IsContentPartRecord()) return null;

            // The property path can't be empty.
            if (string.IsNullOrEmpty(propertyPath)) return null;

            // The starting point of the reference chain is the ContentPartRecord defined by the binding.
            var currentRecordType = bindingRecordType;

            // Unpack the property path defined the binding to be able to find the last property that will represent a 1-to-many connection.
            var propertyNames = propertyPath.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            // This will store the names of the properties that represent a 1-to-many connection.
            var recordListReferencePropertyNames = new List<string>();

            // Walking through the property path until we encounter a halting condition.
            foreach (var propertyName in propertyNames)
            {
                // If the current record type is invalid for some reason, then do nothing.
                if (currentRecordType == null) return null;

                // Fetching the next property in the path from the current record type.
                var property = currentRecordType.GetProperty(propertyName);

                // If the property is not found, then do nothing.
                if (property == null) return null;

                // If a property's type is a generic and it also implements IEnumerable, then it represents a 1-to-many connection.
                if (property.PropertyType.IsGenericType &&
                    property.PropertyType.GetInterfaces().Any(@interface => @interface.Name == typeof(IEnumerable<>).Name))
                {
                    recordListReferencePropertyNames.Add(propertyName);
                    currentRecordType = property.PropertyType.GetGenericArguments().FirstOrDefault();
                }
                // If the property doesn't represent a 1-to-many connection,
                // then we'll just use it to filter (although it can still represent a table join for a single record).
                else break;
            }

            return recordListReferencePropertyNames;
        }

        public static void GetChainableMemberBindingAlias(
            IAliasFactory alias,
            Type bindingRecordType,
            IEnumerable<string> recordListReferencePropertyNames,
            string filterPropertyName,
            string value = "")
        {
            // The starting point of the alias needs to be a ContentPart(Version)Record.
            if (!bindingRecordType.IsContentPartRecord()) return;

            // The property path to finally filter with can't be empty.
            if (string.IsNullOrEmpty(filterPropertyName)) return;

            // Start with a join on the initial ContentPart(Version)Record.
            alias = alias.ContentPartRecord(bindingRecordType);

            if (recordListReferencePropertyNames.Any())
            {
                // Go through all the properties except the last one, because the last join will get a unique alias.
                foreach (var name in recordListReferencePropertyNames.Take(recordListReferencePropertyNames.Count() - 1))
                    // And then join the current table represented by the property.
                    alias = alias.Property(name, name.HtmlClassify());

                // Then create a unique alias for the last join using the remaining property path and value.
                alias.Property(recordListReferencePropertyNames.Last(), $"{filterPropertyName}.{value}".HtmlClassify()); 
            }
        }
    }
}