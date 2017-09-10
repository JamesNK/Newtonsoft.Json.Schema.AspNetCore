#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Newtonsoft.Json.Schema.AspNetCore.Infrastructure
{
    internal static class ValidationHelper
    {
        public static IList<ValidationError> Validate(Stream reader, JSchema schema)
        {
            IList<ValidationError> validationErrors = new List<ValidationError>();

            using (StreamReader bufferReader = new StreamReader(reader, Encoding.UTF8, true, 1024, leaveOpen: true))
            {
                JSchemaValidatingReader validatingReader = new JSchemaValidatingReader(new JsonTextReader(bufferReader));
                validatingReader.Schema = schema;
                validatingReader.ValidationEventHandler += (sender, args) =>
                {
                    validationErrors.Add(args.ValidationError);
                };

                while (validatingReader.Read())
                {
                }
            }

            return validationErrors;
        }
    }
}
