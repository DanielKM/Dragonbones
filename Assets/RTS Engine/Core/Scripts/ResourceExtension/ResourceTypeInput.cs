using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSEngine.ResourceExtension
{
    [System.Serializable]
    public struct ResourceTypeInput
    {
        public ResourceTypeInfo type;
        public ResourceTypeValue value;
    }
}
