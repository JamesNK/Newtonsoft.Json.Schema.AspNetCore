#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Newtonsoft.Json.Schema.AspNetCore.Infrastructure
{
    internal static class ReflectionHelpers
    {
        public static bool InheritsGenericDefinition(Type currentType, Type genericClassDefinition, out Type implementingType)
        {
            if (currentType.IsGenericType)
            {
                Type currentGenericClassDefinition = currentType.GetGenericTypeDefinition();

                if (genericClassDefinition == currentGenericClassDefinition)
                {
                    implementingType = currentType;
                    return true;
                }
            }

            if (currentType.BaseType == null)
            {
                implementingType = null;
                return false;
            }

            return InheritsGenericDefinition(currentType.BaseType, genericClassDefinition, out implementingType);
        }
    }
}
