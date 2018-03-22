using System;
using System.Collections.Generic;
using System.Text;

namespace Chic.ChangeTracking
{
    public interface IProxyChanges
    {
        bool IsModified { get; set; }

        Dictionary<string, object> OriginalValues { get; set; }
    }
}
