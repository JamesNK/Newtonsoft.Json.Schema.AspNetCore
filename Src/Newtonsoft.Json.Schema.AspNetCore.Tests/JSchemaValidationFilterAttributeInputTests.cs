#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema.AspNetCore.Tests.Fakes;
using Xunit;

namespace Newtonsoft.Json.Schema.AspNetCore.Tests
{
    public class JSchemaValidationFilterAttributeInputTests
    {
        [Fact]
        public async Task OnResourceExecutionAsync_ObjectInput_GenerateObjectSchema()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResourceExecutionAsync(attribute, typeof(string));

            Assert.Equal(typeof(string), fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_JTokenInput_SkipSchemaGeneration()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResourceExecutionAsync(attribute, typeof(JToken));

            Assert.Equal(null, fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_NonBodyParameter_SkipSchemaGeneration()
        {
            FakeSchemaGenerator fakeSchemaGenerator = new FakeSchemaGenerator();

            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();
            attribute.SchemaGenerator = fakeSchemaGenerator;

            await CallOnResourceExecutionAsync(attribute, bindingSource: BindingSource.Header);

            Assert.Equal(null, fakeSchemaGenerator.GenerateSchemaType);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_InvalidBody_IsNotValid()
        {
            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();

            ResourceExecutedContext context = await CallOnResourceExecutionAsync(attribute, typeof(string));

            Assert.False(context.ModelState.IsValid);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_InvalidBody_ValidationErrorRaised()
        {
            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();

            ResourceExecutedContext context = await CallOnResourceExecutionAsync(attribute, typeof(string));

            Assert.Equal(1, context.ModelState["input"].Errors.Count);
            Assert.IsType<JSchemaValidationErrorsException>(context.ModelState["input"].Errors[0].Exception);

            JSchemaValidationErrorsException exception = (JSchemaValidationErrorsException)context.ModelState["input"].Errors[0].Exception;
            Assert.Equal(1, exception.SchemaValidationErrors.Count);

            Assert.Equal("Invalid type. Expected String but got Object.", exception.SchemaValidationErrors[0].Message);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_BodyValidation_BodyPositionReset()
        {
            JSchemaValidationFilterAttribute attribute = new JSchemaValidationFilterAttribute();

            HttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("'text'"));

            await CallOnResourceExecutionAsync(
                attribute,
                typeof(string),
                nextCallback: () =>
                {
                    Assert.Equal(0, httpContext.Request.Body.Position);
                    return Task.CompletedTask;
                },
                httpContext: httpContext);
        }

        private static async Task<ResourceExecutedContext> CallOnResourceExecutionAsync(
            JSchemaValidationFilterAttribute attribute,
            Type parameterType = null,
            BindingSource bindingSource = null,
            Func<Task> nextCallback = null,
            HttpContext httpContext = null)
        {
            if (httpContext == null)
            {
                httpContext = new DefaultHttpContext();
                httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
            }

            ResourceExecutingContext context = new ResourceExecutingContext(
                new ActionContext
                {
                    ActionDescriptor = new ActionDescriptor
                    {
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor
                            {
                                BindingInfo = new BindingInfo
                                {
                                    BindingSource = bindingSource ?? BindingSource.Body
                                },
                                Name = "input",
                                ParameterType = parameterType ?? typeof(object)
                            }
                        }
                    },
                    RouteData = new RouteData(),
                    HttpContext = httpContext
                },
                new List<IFilterMetadata>(),
                new List<IValueProviderFactory>());

            ResourceExecutedContext c = new ResourceExecutedContext(context, context.Filters);

            await attribute.OnResourceExecutionAsync(
                context,
                async () =>
                {
                    if (nextCallback != null)
                    {
                        await nextCallback();
                    }
                    
                    return c;
                });

            return c;
        }
    }
}
