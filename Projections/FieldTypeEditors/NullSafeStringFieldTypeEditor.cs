using Lombiq.Projections.Constants;
using Lombiq.Projections.Projections.Forms;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Projections.Models;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.FieldTypeEditors
{
    /// <summary>
    /// <see cref="IFieldTypeEditor"/> implementation for nullable properties of type <see cref="char"/> and <see cref="string"/>.
    /// </summary>
    [OrchardFeature(FeatureNames.Fields)]
    public class NullSafeStringFieldTypeEditor : INullSafeFieldTypeEditor
    {
        public Localizer T { get; set; }


        public NullSafeStringFieldTypeEditor()
        {
            T = NullLocalizer.Instance;
        }


        public bool CanHandle(Type storageType) =>
            new[] { typeof(string), typeof(char) }.Contains(storageType);

        public string FormName => NullSafeStringFilterForm.FormName;

        public Action<IHqlExpressionFactory> GetFilterPredicate(dynamic formState) =>
            NullSafeStringFilterForm.GetFilterPredicate(formState, "Value");

        public LocalizedString DisplayFilter(string fieldName, string storageName, dynamic formState) =>
            NullSafeStringFilterForm.DisplayFilter(fieldName + " " + storageName, formState, T);

        public Action<IAliasFactory> GetFilterRelationship(string aliasName) =>
            x => x.ContentPartRecord<FieldIndexPartRecord>().Property("StringFieldIndexRecords", aliasName);
    }
}