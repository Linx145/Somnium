// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Somnium.Framework
{
    internal static class HashCodeHelper
    {
        /// <summary>
        /// Combines two hash codes, useful for combining hash codes of individual vector elements
        /// </summary>
        internal static int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }
    }
}
