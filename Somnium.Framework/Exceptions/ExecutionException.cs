﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somnium.Framework
{
    public class ExecutionException : Exception
    {
        public ExecutionException(string errorMessage) : base(errorMessage) { }
    }
}
