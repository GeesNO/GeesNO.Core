using System.Text;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Neutral;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Runtime;
using GeesNO.Reference.Simulator;

namespace GeesNO.Sample.Cli;

/// <summary>
/// Runs the non-normative sample commands against the public GeesNO.Core Runtime facade.
/// </summary>
public sealed class CliApplication
{
    private const int DefaultValidateMaxLength = NeutralValidateOperation.MaximumSupportedLength;
    private const string InvalidUsageCode = "sample.invalid-usage";
    private const string UnexpectedFailureCode = "sample.unexpected-failure";
    private const string UnexpectedFailureMessage = "The sample command failed unexpectedly.";

    private readonly Func<GeesNORuntime> _runtimeFactory;

    /// <summary>
    /// Creates a CLI application that uses the default neutral reference simulator runtime.
    /// </summary>
    /// <param name="runtimeFactory">Optional factory used by tests to compose public Runtime scenarios.</param>
    public CliApplication(Func<GeesNORuntime>? runtimeFactory = null)
    {
        _runtimeFactory = runtimeFactory ?? CreateRuntime;
    }

    /// <summary>
    /// Runs one command and writes only public sanitized output.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="output">Standard output writer.</param>
    /// <param name="error">Standard error writer.</param>
    /// <param name="cancellationToken">Cancellation token propagated to Runtime execution.</param>
    /// <returns>The stable CLI exit code.</returns>
    public async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            if (!TryParse(args, out var command, out var parseError))
            {
                return WriteInvalidUsage(error, parseError);
            }

            if (command.Kind == CommandKind.Help)
            {
                WriteUsage(output);
                return CliExitCodes.Success;
            }

            using var runtime = _runtimeFactory()
                ?? throw new InvalidOperationException("Runtime factory returned null.");

            return command.Kind switch
            {
                CommandKind.Capabilities => WriteCapabilities(runtime, output),
                CommandKind.Echo => await ExecuteEchoAsync(runtime, command, output, error, cancellationToken)
                    .ConfigureAwait(false),
                CommandKind.Describe => await ExecuteDescribeAsync(runtime, command, output, error, cancellationToken)
                    .ConfigureAwait(false),
                CommandKind.Validate => await ExecuteValidateAsync(runtime, command, output, error, cancellationToken)
                    .ConfigureAwait(false),
                _ => WriteInvalidUsage(error, "Unknown command.")
            };
        }
        catch (OperationCanceledException)
        {
            return WriteCancellation(error);
        }
        catch (Exception)
        {
            return WriteUnexpectedFailure(error);
        }
    }

    /// <summary>
    /// Creates the explicit public Runtime composition used by the sample.
    /// </summary>
    /// <returns>A started Runtime facade with the neutral reference simulator registered.</returns>
    public static GeesNORuntime CreateRuntime()
    {
        var providerId = NeutralSimulatorProvider.Id;

        return new GeesNORuntimeBuilder()
            .RegisterManifest(NeutralSimulatorManifestFactory.Create(providerId))
            .RegisterTool(providerId, new NeutralEchoTool())
            .RegisterTool(providerId, new NeutralDescribeTool())
            .RegisterTool(providerId, new NeutralValidateTool())
            .Build();
    }

    private static async Task<int> ExecuteEchoAsync(
        GeesNORuntime runtime,
        ParsedCommand command,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        if (!TryCreateOptions(command.ProviderId, out var options, out var usageError))
        {
            return WriteInvalidUsage(error, usageError);
        }

        NeutralEchoOperation operation;
        try
        {
            operation = new NeutralEchoOperation(OperationId.New(), command.Text!);
        }
        catch (ArgumentException)
        {
            return WriteInvalidUsage(error, "The echo text is outside the public neutral contract limits.");
        }

        return await ExecuteAndWriteAsync<NeutralEchoOperation, NeutralEchoResult>(
            runtime,
            operation,
            options,
            output,
            error,
            static (writer, value) => writer.WriteLine($"value.text={Sanitize(value.Text)}"),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ExecuteDescribeAsync(
        GeesNORuntime runtime,
        ParsedCommand command,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        if (!TryCreateOptions(command.ProviderId, out var options, out var usageError))
        {
            return WriteInvalidUsage(error, usageError);
        }

        var operation = new NeutralDescribeOperation(OperationId.New());

        return await ExecuteAndWriteAsync<NeutralDescribeOperation, NeutralDescribeResult>(
            runtime,
            operation,
            options,
            output,
            error,
            static (writer, value) =>
            {
                writer.WriteLine($"value.providerDescription={Sanitize(value.ProviderDescription)}");
                writer.WriteLine($"value.capabilityDescription={Sanitize(value.CapabilityDescription)}");
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ExecuteValidateAsync(
        GeesNORuntime runtime,
        ParsedCommand command,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        if (!TryCreateOptions(command.ProviderId, out var options, out var usageError))
        {
            return WriteInvalidUsage(error, usageError);
        }

        var maxLength = command.MaxLength ?? DefaultValidateMaxLength;
        NeutralValidateOperation operation;
        try
        {
            operation = new NeutralValidateOperation(OperationId.New(), command.Text!, maxLength);
        }
        catch (ArgumentException)
        {
            return WriteInvalidUsage(error, "The validate input is outside the public neutral contract limits.");
        }

        return await ExecuteAndWriteAsync<NeutralValidateOperation, NeutralValidationResult>(
            runtime,
            operation,
            options,
            output,
            error,
            static (writer, value) =>
            {
                writer.WriteLine($"value.isValid={value.IsValid.ToString().ToLowerInvariant()}");
                if (value.Reason is not null)
                {
                    writer.WriteLine($"value.reason={Sanitize(value.Reason)}");
                }
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ExecuteAndWriteAsync<TOperation, TResult>(
        GeesNORuntime runtime,
        TOperation operation,
        ExecutionOptions? options,
        TextWriter output,
        TextWriter error,
        Action<TextWriter, TResult> writeValue,
        CancellationToken cancellationToken)
        where TOperation : IOperation<TResult>
        where TResult : notnull
    {
        var result = await runtime.ExecuteAsync(operation, options, cancellationToken).ConfigureAwait(false);

        if (result.Status != OperationExecutionStatus.Success)
        {
            return WriteFailure(result, error);
        }

        output.WriteLine("status=success");
        output.WriteLine($"operation={Sanitize(result.Capability.Id.Value)}");
        output.WriteLine($"capability={Sanitize(result.Capability.ToString())}");
        output.WriteLine($"provider={Sanitize(result.ProviderId!.Value)}");
        writeValue(output, result.Value!);
        return CliExitCodes.Success;
    }

    private static int WriteCapabilities(GeesNORuntime runtime, TextWriter output)
    {
        output.WriteLine("status=success");
        output.WriteLine($"capability.count={runtime.Capabilities.Count}");

        for (var index = 0; index < runtime.Capabilities.Count; index++)
        {
            var capability = runtime.Capabilities[index];
            output.WriteLine($"capability.{index}.provider={Sanitize(capability.ProviderId.Value)}");
            output.WriteLine($"capability.{index}.reference={Sanitize(capability.Descriptor.Reference.ToString())}");
            output.WriteLine($"capability.{index}.support={capability.Support.SupportLevel}");
        }

        return CliExitCodes.Success;
    }

    private static int WriteFailure<TResult>(OperationExecutionResult<TResult> result, TextWriter error)
        where TResult : notnull
    {
        error.WriteLine($"status={FormatStatus(result.Status)}");
        error.WriteLine($"capability={Sanitize(result.Capability.ToString())}");
        if (result.ProviderId is not null)
        {
            error.WriteLine($"provider={Sanitize(result.ProviderId.Value)}");
        }

        if (result.Error is not null)
        {
            error.WriteLine($"error.code={Sanitize(result.Error.Code)}");
            error.WriteLine($"error.message={Sanitize(result.Error.Message)}");
        }

        return MapExitCode(result.Status);
    }

    private static int WriteInvalidUsage(TextWriter error, string message)
    {
        error.WriteLine("status=invalid-usage");
        error.WriteLine($"error.code={InvalidUsageCode}");
        error.WriteLine($"error.message={Sanitize(message)}");
        WriteUsage(error);
        return CliExitCodes.InvalidUsage;
    }

    private static int WriteCancellation(TextWriter error)
    {
        error.WriteLine("status=cancelled");
        return CliExitCodes.Cancellation;
    }

    private static int WriteUnexpectedFailure(TextWriter error)
    {
        error.WriteLine("status=unexpected-failure");
        error.WriteLine($"error.code={UnexpectedFailureCode}");
        error.WriteLine($"error.message={UnexpectedFailureMessage}");
        return CliExitCodes.RuntimeFailure;
    }

    private static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("usage=geesno-sample --help");
        writer.WriteLine("usage=geesno-sample capabilities");
        writer.WriteLine("usage=geesno-sample echo <text> [--provider <provider-id>]");
        writer.WriteLine("usage=geesno-sample describe [--provider <provider-id>]");
        writer.WriteLine("usage=geesno-sample validate <text> [--max-length <n>] [--provider <provider-id>]");
        writer.WriteLine($"validate.defaultMaxLength={DefaultValidateMaxLength}");
    }

    private static bool TryParse(
        IReadOnlyList<string> args,
        out ParsedCommand command,
        out string error)
    {
        command = ParsedCommand.Empty;
        error = string.Empty;

        if (args.Count == 0)
        {
            error = "A command is required.";
            return false;
        }

        var commandText = args[0];
        if (commandText is "--help" or "-h")
        {
            if (args.Count != 1)
            {
                error = "Help does not accept additional arguments.";
                return false;
            }

            command = new ParsedCommand(CommandKind.Help, null, null, null);
            return true;
        }

        var kind = commandText switch
        {
            "capabilities" => CommandKind.Capabilities,
            "echo" => CommandKind.Echo,
            "describe" => CommandKind.Describe,
            "validate" => CommandKind.Validate,
            _ => CommandKind.Unknown
        };

        if (kind == CommandKind.Unknown)
        {
            error = "Unknown command.";
            return false;
        }

        string? provider = null;
        int? maxLength = null;
        var positionals = new List<string>();

        for (var index = 1; index < args.Count; index++)
        {
            var token = args[index];
            if (token == "--provider")
            {
                if (provider is not null)
                {
                    error = "Provider option cannot be repeated.";
                    return false;
                }

                if (!TryReadOptionValue(args, ref index, out provider))
                {
                    error = "Provider option requires a value.";
                    return false;
                }

                continue;
            }

            if (token == "--max-length")
            {
                if (kind != CommandKind.Validate)
                {
                    error = "Max length is valid only for validate.";
                    return false;
                }

                if (maxLength is not null)
                {
                    error = "Max length option cannot be repeated.";
                    return false;
                }

                if (!TryReadOptionValue(args, ref index, out var maxLengthText))
                {
                    error = "Max length option requires a value.";
                    return false;
                }

                if (!int.TryParse(maxLengthText, out var parsedMaxLength)
                    || parsedMaxLength is <= 0 or > NeutralValidateOperation.MaximumSupportedLength)
                {
                    error = $"Max length must be an integer from 1 to {NeutralValidateOperation.MaximumSupportedLength}.";
                    return false;
                }

                maxLength = parsedMaxLength;
                continue;
            }

            if (token.StartsWith("--", StringComparison.Ordinal))
            {
                error = "Unknown option.";
                return false;
            }

            positionals.Add(token);
        }

        if (kind == CommandKind.Capabilities && (positionals.Count != 0 || provider is not null || maxLength is not null))
        {
            error = "Capabilities does not accept arguments.";
            return false;
        }

        if (kind == CommandKind.Describe && positionals.Count != 0)
        {
            error = "Describe does not accept positional arguments.";
            return false;
        }

        if ((kind == CommandKind.Echo || kind == CommandKind.Validate) && positionals.Count != 1)
        {
            error = "Command requires exactly one text argument.";
            return false;
        }

        command = new ParsedCommand(kind, positionals.SingleOrDefault(), provider, maxLength);
        return true;
    }

    private static bool TryReadOptionValue(
        IReadOnlyList<string> args,
        ref int index,
        out string value)
    {
        value = string.Empty;

        if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            return false;
        }

        index++;
        value = args[index];
        return true;
    }

    private static bool TryCreateOptions(
        string? providerText,
        out ExecutionOptions? options,
        out string error)
    {
        options = null;
        error = string.Empty;

        if (providerText is null)
        {
            return true;
        }

        if (!ProviderId.TryParse(providerText, out var providerId))
        {
            error = "Provider identifier is not valid.";
            return false;
        }

        options = new ExecutionOptions(providerId);
        return true;
    }

    private static string FormatStatus(OperationExecutionStatus status)
    {
        return status switch
        {
            OperationExecutionStatus.Success => "success",
            OperationExecutionStatus.Rejected => "rejected",
            OperationExecutionStatus.FunctionalFailure => "functional-failure",
            OperationExecutionStatus.ContractMismatch => "contract-mismatch",
            OperationExecutionStatus.UnexpectedFailure => "unexpected-failure",
            OperationExecutionStatus.Cancelled => "cancelled",
            _ => "runtime-failure"
        };
    }

    private static int MapExitCode(OperationExecutionStatus status)
    {
        return status switch
        {
            OperationExecutionStatus.Success => CliExitCodes.Success,
            OperationExecutionStatus.Rejected => CliExitCodes.RejectedExecution,
            OperationExecutionStatus.FunctionalFailure => CliExitCodes.FunctionalFailure,
            OperationExecutionStatus.ContractMismatch => CliExitCodes.RuntimeFailure,
            OperationExecutionStatus.UnexpectedFailure => CliExitCodes.RuntimeFailure,
            OperationExecutionStatus.Cancelled => CliExitCodes.Cancellation,
            _ => CliExitCodes.RuntimeFailure
        };
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsControl(character) ? ' ' : character);
        }

        return RedactWindowsPathLikeTokens(builder.ToString().Trim());
    }

    private static string RedactWindowsPathLikeTokens(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.None);
        for (var index = 0; index < parts.Length; index++)
        {
            if (LooksLikeWindowsPath(parts[index]))
            {
                parts[index] = "[path]";
            }
        }

        return string.Join(' ', parts);
    }

    private static bool LooksLikeWindowsPath(string value)
    {
        return value.Length >= 3
               && char.IsAsciiLetter(value[0])
               && value[1] == ':'
               && (value[2] == '\\' || value[2] == '/');
    }

    private sealed record ParsedCommand(
        CommandKind Kind,
        string? Text,
        string? ProviderId,
        int? MaxLength)
    {
        internal static ParsedCommand Empty { get; } = new(CommandKind.Unknown, null, null, null);
    }

    private enum CommandKind
    {
        Unknown,
        Help,
        Capabilities,
        Echo,
        Describe,
        Validate
    }
}
