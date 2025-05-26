using System;
using System.Runtime.Serialization;

namespace Monq.Core.ClickHouseBuffer.Exceptions;

/// <summary>
/// Represents errors that occur when the buffer is incorrectly configured.
/// </summary>
public sealed class BufferConfigurationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="BufferConfigurationException" /> class.</summary>
    public BufferConfigurationException()
    {

    }

    /// <summary>Initializes a new instance of the <see cref="BufferConfigurationException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public BufferConfigurationException(string message) : base(message)
    {

    }

    /// <summary>Initializes a new instance of the <see cref="BufferConfigurationException" /> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
    public BufferConfigurationException(string message, Exception innerException) : base(message, innerException)
    {

    }
}

