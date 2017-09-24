using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Schema.AspNetCore.Infrastructure.Model;
using Newtonsoft.Json.Schema.AspNetCore.Tests.Fakes;
using Xunit;

namespace Newtonsoft.Json.Schema.AspNetCore.Tests.Infrastructure.Model
{
    public class ActionValidationModelTests
    {
        [Fact]
        public void Create_InvalidReturnType_NoResponseTypes()
        {
            ActionValidationModel model = ActionValidationModel.Create(new ControllerActionDescriptor
                {
                    MethodInfo = new Func<IActionResult>(() => null).Method,
                },
                new JSchemaValidationFilterAttribute(),
                new FakeHostingEnvironment());

            Assert.Equal(0, model.ResponseTypes.Count);
            Assert.False(model.BufferResponse);
        }

        [Fact]
        public void Create_ValidReturnType_200ResponseType()
        {
            ActionValidationModel model = ActionValidationModel.Create(new ControllerActionDescriptor
                {
                    MethodInfo = new Func<string>(() => null).Method
                },
                new JSchemaValidationFilterAttribute(),
                new FakeHostingEnvironment());

            Assert.Equal(1, model.ResponseTypes.Count);
            Assert.Equal(200, model.ResponseTypes[0].StatusCode);
            Assert.Equal(typeof(string), model.ResponseTypes[0].Type);
            Assert.True(model.BufferResponse);
        }

        [Fact]
        public void Create_InvalidProducesResponseTypeAttributeTypeOverridesValidReturnType_NoResponseTypes()
        {
            ActionValidationModel model = ActionValidationModel.Create(new ControllerActionDescriptor
                {
                    MethodInfo = new Func<string>(() => null).Method,
                    FilterDescriptors = new List<FilterDescriptor>
                    {
                        new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(void), 200), 1)
                    }
                },
                new JSchemaValidationFilterAttribute(),
                new FakeHostingEnvironment());

            Assert.Equal(0, model.ResponseTypes.Count);
            Assert.False(model.BufferResponse);
        }

        [Fact]
        public void Create_ValidProducesResponseTypeAttributeTypeOverridesValidReturnType_200ResponseType()
        {
            ActionValidationModel model = ActionValidationModel.Create(new ControllerActionDescriptor
                {
                    MethodInfo = new Func<string>(() => null).Method,
                    FilterDescriptors = new List<FilterDescriptor>
                    {
                        new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(int), 200), 1)
                    }
                },
                new JSchemaValidationFilterAttribute(),
                new FakeHostingEnvironment());

            Assert.Equal(1, model.ResponseTypes.Count);
            Assert.Equal(200, model.ResponseTypes[0].StatusCode);
            Assert.Equal(typeof(int), model.ResponseTypes[0].Type);
            Assert.True(model.BufferResponse);
        }
    }
}
