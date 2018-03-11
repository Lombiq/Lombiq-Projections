namespace Lombiq.Projections.Helpers
{
    public static class FieldIndexHelper
    {
        public static string GetPropertyName(string partName, string fieldName, string valueName = "") =>
            string.Join(".", partName, fieldName, valueName);
    }
}