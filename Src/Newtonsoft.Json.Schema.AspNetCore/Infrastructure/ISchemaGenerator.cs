#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;

namespace Newtonsoft.Json.Schema.AspNetCore.Infrastructure
{
    internal interface ISchemaGenerator
    {
        JSchema GetGenerateSchema(Type type);
    }
}