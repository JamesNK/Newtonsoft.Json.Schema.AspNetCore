#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace Newtonsoft.Json.Schema.AspNetCore.Tests.Fakes
{
    public class FakeServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            return null;
        }
    }
}