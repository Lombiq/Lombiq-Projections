using Lombiq.Projections.Projections.FieldTypeEditors;
using Lombiq.Projections.Projections.Forms;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Projections.Models;
using System;
using System.Linq;

namespace Orchard.Projections.FieldTypeEditors
{
    /// <summary>
    /// <see cref="IFieldTypeEditor"/> implementation for string-based nullable properties.
    /// </summary>
    [OrchardFeature("Lombiq.Projections.Fields")]
    public class NullSafeStringFieldTypeEditor : INullSafeFieldTypeEditor
    {
        public Localizer T { get; set; }


        public NullSafeStringFieldTypeEditor() {
            T = NullLocalizer.Instance;
        }


        public bool CanHandle(Type storageType) {
            return new[] { typeof(string), typeof(char) }.Contains(storageType);
        }

        public string FormName {
            get { return NullSafeContentFieldsFilterForm.FormName; }
        }

        public Action<IHqlExpressionFactory> GetFilterPredicate(dynamic formState) {
            return NullSafeContentFieldsFilterForm.GetFilterPredicate(formState, "Value");
        }

        public LocalizedString DisplayFilter(string fieldName, string storageName, dynamic formState) {
            return NullSafeContentFieldsFilterForm.DisplayFilter(fieldName + " " + storageName, formState, T);
        }

        public Action<IAliasFactory> GetFilterRelationship(string aliasName) {
            return x => x.ContentPartRecord<FieldIndexPartRecord>().Property("StringFieldIndexRecords", aliasName);
        }
    }
}