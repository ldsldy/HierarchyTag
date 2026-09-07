using System.Collections.Generic;

namespace Deukyeonglee.HierarchyTags.Contracts
{
    public interface ITagCatalog
    {
        IReadOnlyList<TagInfo> Tags { get; }

        bool Contains(TagId tag);

        bool TryGetInfo(TagId tag, out TagInfo info);

        bool TryResolve(string value, out TagId tag);
    }
}