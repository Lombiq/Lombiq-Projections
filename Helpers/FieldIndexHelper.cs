using Orchard.Projections.Models;
using System;

namespace Lombiq.Projections.Helpers
{
    public static class FieldIndexHelper
    {
        public static string GetPropertyName(string partName, string fieldName, string valueName = "") =>
            string.Join(".", partName, fieldName, valueName);

        public static Type GetFieldIndexRecordType(Type valueType)
        {
            var typeCode = Type.GetTypeCode(valueType);

            if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Nullable<>))
                typeCode = Type.GetTypeCode(Nullable.GetUnderlyingType(valueType));

            switch (typeCode)
            {
                case TypeCode.Char:
                case TypeCode.String:
                    return typeof(StringFieldIndexRecord);

                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.DateTime:
                case TypeCode.Boolean:
                    return typeof(IntegerFieldIndexRecord);

                case TypeCode.Decimal:
                    return typeof(DecimalFieldIndexRecord);

                case TypeCode.Single:
                case TypeCode.Double:
                    return typeof(DoubleFieldIndexRecord);
            }

            return null;
        }

        public static string GetFieldIndexRecordPropertyName(Type valueType)
        {
            var recordType = GetFieldIndexRecordType(valueType);

            if (recordType == null) return null;

            if (recordType == typeof(StringFieldIndexRecord)) return nameof(FieldIndexPartRecord.StringFieldIndexRecords);
            if (recordType == typeof(IntegerFieldIndexRecord)) return nameof(FieldIndexPartRecord.IntegerFieldIndexRecords);
            if (recordType == typeof(DecimalFieldIndexRecord)) return nameof(FieldIndexPartRecord.DecimalFieldIndexRecords);
            if (recordType == typeof(DoubleFieldIndexRecord)) return nameof(FieldIndexPartRecord.DoubleFieldIndexRecords);

            return null;
        }
    }
}