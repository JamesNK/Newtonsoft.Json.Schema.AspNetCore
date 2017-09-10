#region License
// Copyright (c) Newtonsoft. All Rights Reserved.
// License: https://raw.github.com/JamesNK/Newtonsoft.Json.Schema.AspNetCore/master/LICENSE.md
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Newtonsoft.Json.Schema.AspNetCore
{
    /// <summary>
    /// The exception thrown when validation errors are raised.
    /// </summary>
    public class JSchemaValidationErrorsException : Exception
    {
        /// <summary>
        /// Gets the <see cref="JSchemaValidationErrorsException"/>'s validation errors.
        /// </summary>
        public IList<ValidationError> SchemaValidationErrors { get; } = new List<ValidationError>();

        /// <inheritdoc />
        public JSchemaValidationErrorsException()
        {
        }

        /// <inheritdoc />
        public JSchemaValidationErrorsException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public JSchemaValidationErrorsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <inheritdoc />
        protected JSchemaValidationErrorsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        internal static JSchemaValidationErrorsException Create(IList<ValidationError> validationErrors)
        {
            JSchemaValidationErrorsException ex = new JSchemaValidationErrorsException();
            for (int i = 0; i < validationErrors.Count; i++)
            {
                ex.SchemaValidationErrors.Add(validationErrors[i]);
            }

            return ex;
        }
    }
}