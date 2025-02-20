using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Core.Utility;

/// <summary>
/// Utilties for executing Processes on the Current OS.
/// </summary>
public class ProcessUtility
{
    /// <summary>
    /// Execute a Process with the Specified Args and support for redirecting Outputs back to the caller.
    /// </summary>
    /// <param name="fileName">Executable Name</param>
    /// <param name="processArgs">Argument List</param>
    /// <param name="workingDirectory">Working Directory (null for current directory)</param>
    /// <param name="progressString">Progress Object to Rreceive Standard IO </param>
    /// <param name="token">Cancellation Token to Abort Process if needed</param>
    public static void RunProcessAsync(string fileName, string processArgs, string workingDirectory,
                IProgress<CommandOutput> progressString,
                CancellationToken token)
    {
        using var process = CreateProcess(fileName, processArgs, workingDirectory);

        process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            progressString.Report(new CommandOutput() { Data = e.Data, Error = false });
        });

        process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            progressString.Report(new CommandOutput() { Data = e.Data, Error = true });
        });

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for Exit of Process
        try
        {
            process.WaitForExitAsync(token).GetAwaiter().GetResult();
        }
        catch (TaskCanceledException)
        {
            progressString.Report(new CommandOutput() { Data = "Process Cancelled", Error = true });
        }

        if (token.IsCancellationRequested)
            process.Kill();

        process.Close();
    }

    /// <summary>
    /// Execute a Synchronous Process with the Specified Args and return output on completion.
    /// </summary>
    /// <param name="fileName">Executable Name</param>
    /// <param name="processArgs">Argument List</param>
    /// <param name="workingDirectory">Working Directory (null for current directory)</param>
    /// <param name="result">Output string from stdout</param>
    /// <param name="error">Output string from stderr</param>
    /// <param name="timeoutMs">The timeout in Milliseconds for the process that is going to be run.</param>
    public static bool RunProcess(string fileName, string processArgs, string workingDirectory,
        out string result, out string error, int timeoutMs = -1)
    {
        using var process = CreateProcess(fileName, processArgs, workingDirectory);

        process.Start();
        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill(true);
            result = "";
            error = $"The process ({fileName} {processArgs}) timed out. WorkingDir: {workingDirectory}";
            return false;
        }
        result = process.StandardOutput.ReadToEnd();
        error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return true;
    }

    /// <summary>
    /// Execute a Process in a separate window that can blocking or non-blocking.
    /// </summary>
    /// <param name="fileName">Executable Name</param>
    /// <param name="processArgs">Argument List</param>
    /// <param name="blocking">True for a blocking process, false for non-blocking</param>
    /// <param name="workingDirectory">Overrides the default working directory</param>
    public static void RunProcessInWindow(string fileName, string processArgs = null, bool blocking = false, string workingDirectory = null)
    {
        using var process = CreateProcessInWindow(fileName, processArgs, workingDirectory);
        process.Start();
        if (blocking)
            process.WaitForExit();
    }

    /// <summary>
    /// Execute a Synchronous Process using the platform shell.
    /// </summary>
    /// <param name="script">Command script to run</param>
    /// <param name="workingDirectory">Working Directory (null for current directory)</param>
    /// <param name="result">Output string from stdout</param>
    /// <param name="error">Output string from stderr</param>
    public static void RunProcessScript(string script, string workingDirectory, out string result, out string error)
    {
        string tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, script);
        ProcessUtility.RunProcess("bash", tempFile, workingDirectory, out string stdout, out string stderr);
        File.Delete(tempFile);
        result = stdout;
        error = stderr;
    }

    /// <summary>
    /// Creates a base Process with the Specified Args.
    /// </summary>
    /// <param name="fileName">Executable Name</param>
    /// <param name="processArgs">Argument List</param>
    /// <param name="workingDirectory">Working Directory (null for current directory)</param>
    public static Process CreateProcess(string fileName, string processArgs = null, string workingDirectory = null)
    {
        Process process = new();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = processArgs;
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        return process;
    }

    /// <summary>
    /// Creates a base Windowed Process with the Specified Args.
    /// </summary>
    /// <param name="fileName">Executable Name</param>
    /// <param name="processArgs">Argument List</param>
    /// <param name="workingDirectory">Overrides the default working directory</param>
    public static Process CreateProcessInWindow(string fileName, string processArgs = null, string workingDirectory = null)
    {
        Process process = new();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = processArgs;
        process.StartInfo.WorkingDirectory = workingDirectory ?? string.Empty;
        process.StartInfo.UseShellExecute = true;
        return process;
    }
}

/// <summary>
/// Return object for async process messages.
/// </summary>
public class CommandOutput
{
    /// <summary>
    /// Command Output
    /// </summary>
    public string Data;

    /// <summary>
    ///  Property indicating if data is from StdErr.
    /// </summary>
    public bool Error;
}