using Lombiq.Projections.Constants;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;
using Orchard.Data.Conventions;
using Orchard.Environment.Extensions;
using System.Collections.Generic;

namespace Lombiq.Projections.Models
{
    /// <summary>
    /// Similar to <see cref="Orchard.Taxonomies.Models.TermsPart"/>, this class links
    /// content items to Terms using <see cref="TitleSortableTermContentItem"/>,
    /// which includes additional information necessary for sorting content items.
    /// </summary>
    [OrchardFeature(FeatureNames.Taxonomies)]
    public class TitleSortableTermsPart : ContentPart<TitleSortableTermsPartRecord>
    {
        public IList<TitleSortableTermContentItem> Terms
        {
            get { return Record.Terms; }
            set { Record.Terms = value; }
        }
    }


    [OrchardFeature(FeatureNames.Taxonomies)]
    public class TitleSortableTermsPartRecord : ContentPartRecord
    {
        public TitleSortableTermsPartRecord()
        {
            Terms = new List<TitleSortableTermContentItem>();
        }

        [CascadeAllDeleteOrphan]
        public virtual IList<TitleSortableTermContentItem> Terms { get; set; }
    }
}