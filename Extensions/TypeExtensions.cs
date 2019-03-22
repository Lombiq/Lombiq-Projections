using Orchard.ContentManagement.Records;
using System;

namespace Lombiq.Projections.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsContentPartRecord(this Type @type) =>
            @type.BaseType == typeof(ContentPartRecord) || IsContentPartVersionRecord(@type);

        public static bool IsContentPartVersionRecord(this Type @type) =>
            @type.BaseType == typeof(ContentPartVersionRecord);
    }
}