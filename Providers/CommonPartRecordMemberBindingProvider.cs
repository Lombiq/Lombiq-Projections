using Orchard.Core.Common.Models;
using Orchard.Localization;
using System.Collections.Generic;

namespace Lombiq.Projections.Providers
{
    public class CommonPartRecordMemberBindingProvider : IChainableMemberBindingProvider
    {
        public Localizer T { get; set; }


        public CommonPartRecordMemberBindingProvider()
        {
            T = NullLocalizer.Instance;
        }


        public IEnumerable<ChainableMemberBinding> GetChainableMemberBindings() =>
            new[]
            {
                new ChainableMemberBinding
                {
                    ContentPartRecordType = typeof(CommonPartRecord),
                    PropertyPath = $"{nameof(CommonPartRecord.OwnerId)}",
                    DisplayName = T("Owner ID"),
                    Description = T("The User ID of the content item's owner.")
                }
            };
    }
}