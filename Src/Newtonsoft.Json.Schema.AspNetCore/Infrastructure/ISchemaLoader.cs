#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using Microsoft.AspNetCore.Hosting;

namespace Newtonsoft.Json.Schema.AspNetCore.Infrastructure
{
    internal interface ISchemaLoader
    {
        JSchema GetLoadedSchema(IHostingEnvironment hostingEnvironment, string path);
    }
}