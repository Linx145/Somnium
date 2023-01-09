using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somnium.Framework
{
    public class InitializationException : Exception
    {
        public InitializationException(string errorMessage) : base(errorMessage) { }
    }
}
