using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BEPUutilities
{
    public class NotFiniteNumberException : Exception
    {
        public NotFiniteNumberException(string message) : base(message) { }
    }
}
