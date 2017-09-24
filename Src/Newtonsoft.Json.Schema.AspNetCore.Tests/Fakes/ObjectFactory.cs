#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Schema.AspNetCore.Infrastructure;

namespace Newtonsoft.Json.Schema.AspNetCore.Tests.Fakes
{
    public static class ObjectFactory
    {
        public static HttpContext CreateHttpContext(byte[] bodyData = null, string responseContentType = null)
        {
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(bodyData ?? Encoding.UTF8.GetBytes("{}"));
            httpContext.Response.Body = new MemoryStream();
            httpContext.Response.ContentType = responseContentType ?? Constants.ContentTypes.ApplicationJson;
            httpContext.RequestServices = new FakeServiceProvider();

            return httpContext;
        }
    }
}
