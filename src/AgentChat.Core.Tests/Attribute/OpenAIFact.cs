﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// OpenAIFact.cs

namespace AgentChat.Core.Tests;

/// <summary>
///     A fact for tests requiring OPENAI_API_KEY env.
/// </summary>
public sealed class ApiKeyFactAttribute : EnvironmentSpecificFactAttribute
{
    private readonly string _envVariableName;

    public ApiKeyFactAttribute(string envVariableName = "OPENAI_API_KEY") : base($"{envVariableName} is not found in env")
    {
        _envVariableName = envVariableName;
    }

    /// <inheritdoc />
    protected override bool IsEnvironmentSupported() => Environment.GetEnvironmentVariables().Contains(_envVariableName);
}