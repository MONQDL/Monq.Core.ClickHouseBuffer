using System;
using System.Collections.Generic;

namespace Monq.Core.ClickHouseBuffer.Exceptions
{
    public class PersistingException : Exception
    {
        /// <summary>
        /// The data values.
        /// </summary>
        public IDictionary<string, object>[] DbValues { get; }

        /// <summary>
        /// The table name in whitch the data must be inserted.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistingException" /> 
        /// class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, 
        /// or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.
        /// <param name="dbValues">The data values.</param>
        /// <param name="tableName">The table name in whitch the data must be inserted.</param>
        /// </param>
        public PersistingException(string message,
            IDictionary<string, object>[] dbValues,
            string tableName,
            Exception innerException) : base(message, innerException)
        {
            DbValues = dbValues;
            TableName = tableName;
        }
    }
}
