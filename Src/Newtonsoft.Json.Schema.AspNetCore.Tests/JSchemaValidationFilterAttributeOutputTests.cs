#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema.AspNetCore.Tests.Fakes;
using Xunit;

namespace Newtonsoft.Json.Schema.AspNetCore.Tests
{
    public class JSchemaValidationFilterAttributeOutputTests
    {
        [Fact]
        public async Task OnResultExecutionAsync_ObjectResult_GenerateObjectSchema()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResultExecutionAsync(attribute);

            Assert.Equal(typeof(object), fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResultExecutionAsync_StringResult_GenerateStringSchema()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResultExecutionAsync(attribute, new Func<string>(() => "").Method);

            Assert.Equal(typeof(string), fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResultExecutionAsync_GenericTaskWithStringResult_GenerateStringSchema()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResultExecutionAsync(attribute, new Func<Task<string>>(() => Task.FromResult(string.Empty)).Method);

            Assert.Equal(typeof(string), fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResultExecutionAsync_JTokenResult_SkipSchemaGeneration()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResultExecutionAsync(attribute, new Func<JToken>(() => "").Method);

            Assert.Equal(null, fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResultExecutionAsync_ActionResult_SkipSchemaGeneration()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResultExecutionAsync(attribute, new Func<IActionResult>(() => new EmptyResult()).Method);

            Assert.Equal(null, fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResultExecutionAsync_TaskResult_SkipSchemaGeneration()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResultExecutionAsync(attribute, new Func<Task>(() => Task.CompletedTask).Method);

            Assert.Equal(null, fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResultExecutionAsync_GenericTaskWithJTokenResult_SkipSchemaGeneration()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResultExecutionAsync(attribute, new Func<Task<JToken>>(() => Task.FromResult<JToken>(new JObject())).Method);

            Assert.Equal(null, fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResultExecutionAsync_GenericTaskWithActionResult_SkipSchemaGeneration()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResultExecutionAsync(attribute, new Func<Task<IActionResult>>(() => Task.FromResult<IActionResult>(new EmptyResult())).Method);

            Assert.Equal(null, fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResultExecutionAsync_VoidResult_SkipSchemaGeneration()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResultExecutionAsync(attribute, new Action(() => { }).Method);

            Assert.Equal(null, fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResultExecutionAsync_InvalidResult_Error()
        {
            const string content = "123";

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();

            HttpContext httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            JSchemaValidationErrorsException ex = await Assert.ThrowsAsync<JSchemaValidationErrorsException>(() => CallOnResultExecutionAsync(attribute, new Func<string>(() => string.Empty).Method, "123", httpContext));

            Assert.Equal("Invalid type. Expected String but got Integer.", ex.SchemaValidationErrors[0].Message);

            Assert.Equal(3, httpContext.Response.Body.Position);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            string resultContent = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();

            Assert.Equal(content, resultContent);
        }

        [Fact]
        public async Task OnResultExecutionAsync_ValidResult_ResetResponseStream()
        {
            const string content = "123";

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();

            HttpContext httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            ResultExecutedContext result = await CallOnResultExecutionAsync(attribute, new Func<int>(() => int.MaxValue).Method, content, httpContext);

            Assert.Equal(3, result.HttpContext.Response.Body.Position);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            string resultContent = await new StreamReader(result.HttpContext.Response.Body).ReadToEndAsync();

            Assert.Equal(content, resultContent);
        }

        private static async Task<ResultExecutedContext> CallOnResultExecutionAsync(
            JSchemaValidationFilterAttribute attribute,
            MethodInfo methodInfo = null,
            string content = null,
            HttpContext httpContext = null)
        {
            httpContext = httpContext ?? new DefaultHttpContext();

            EmptyResult actionResult = new EmptyResult();
            object controller = new object();

            ResultExecutingContext context = new ResultExecutingContext(
                new ActionContext
                {
                    ActionDescriptor = new ControllerActionDescriptor
                    {
                        MethodInfo = methodInfo ?? typeof(JSchemaValidationFilterAttributeOutputTests).GetMethod(nameof(DummyReturnMethod)),
                        
                    },
                    RouteData = new RouteData(),
                    HttpContext = httpContext
                },
                new List<IFilterMetadata>(),
                actionResult,
                controller);

            ResultExecutedContext c = new ResultExecutedContext(context, context.Filters, actionResult, controller);

            await attribute.OnResultExecutionAsync(
                context,
                () =>
                {
                    c.HttpContext.Response.WriteAsync(content ?? "{}");
                    return Task.FromResult(c);
                });

            return c;
        }

        public object DummyReturnMethod()
        {
            return new object();
        }
    }
}
