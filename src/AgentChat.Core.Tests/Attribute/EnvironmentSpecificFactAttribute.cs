﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// EnvironmentSpecificFactAttribute.cs

using Xunit;

namespace AgentChat.Core.Tests;

/// <summary>
///     A base class for environment-specific fact attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public abstract class EnvironmentSpecificFactAttribute : FactAttribute
{
    private readonly string _skipMessage;

    public sealed override string Skip => IsEnvironmentSupported() ? null : _skipMessage;

    /// <summary>
    ///     Creates a new instance of the <see cref="EnvironmentSpecificFactAttribute" /> class.
    /// </summary>
    /// <param name="skipMessage">The message to be used when skipping the test marked with this attribute.</param>
    protected EnvironmentSpecificFactAttribute(string skipMessage)
    {
        _skipMessage = skipMessage ?? throw new ArgumentNullException(nameof(skipMessage));
    }

    /// <summary>
    ///     A method used to evaluate whether to skip a test marked with this attribute. Skips iff this method evaluates to
    ///     false.
    /// </summary>
    protected abstract bool IsEnvironmentSupported();
}