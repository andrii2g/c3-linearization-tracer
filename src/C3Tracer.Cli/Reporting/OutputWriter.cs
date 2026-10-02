using System.Text;

namespace C3Tracer.Cli.Reporting;

public sealed record OutputPaths(string Input, string? Json, string? Svg);

public static class OutputWriter
{
    private static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static OutputPaths Preflight(string input, string? json, string? svg, bool force)
    {
        string fullInput = Resolve(input);
        string? fullJson = json is null ? null : Path.GetFullPath(json);
        string? fullSvg = svg is null ? null : Path.GetFullPath(svg);
        var destinations = new HashSet<string>(PathComparer);
        foreach (string path in new[] { fullJson, fullSvg }.OfType<string>())
        {
            if (new FileInfo(path).LinkTarget is not null) throw new IOException($"Symlink output destination is not allowed: {path}");
            string resolved = Resolve(path);
            if (PathComparer.Equals(resolved, fullInput)) throw new IOException("Output must not alias input.");
            if (!destinations.Add(resolved)) throw new IOException("Output destinations must not alias each other.");
            if (Directory.Exists(path)) throw new IOException($"Output is a directory: {path}");
            if (File.Exists(path) && !force) throw new IOException($"Output exists; use --force: {path}");
            for (string? parent = Path.GetDirectoryName(path); parent is not null; parent = Path.GetDirectoryName(parent))
                if (File.Exists(parent)) throw new IOException($"Output parent is a file: {parent}");
        }
        return new(Path.GetFullPath(input), fullJson, fullSvg);
    }

    // Resolve each existing ancestor too, so directory symlink aliases are detected.
    private static string Resolve(string path)
    {
        string full = Path.GetFullPath(path);
        string root = Path.GetPathRoot(full) ?? throw new IOException("Path has no root.");
        string resolved = root;
        foreach (string part in full[root.Length..].Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            resolved = Path.Combine(resolved, part);
            FileSystemInfo info = Directory.Exists(resolved) ? new DirectoryInfo(resolved) : new FileInfo(resolved);
            if (info.LinkTarget is not null)
                resolved = info.ResolveLinkTarget(true)?.FullName ?? throw new IOException("Cannot resolve symbolic link.");
        }
        return Path.TrimEndingDirectorySeparator(resolved);
    }

    public static async Task WriteAsync(OutputPaths paths, string? json, string? svg, bool force)
    {
        var staged = new List<(string Temp, string Destination)>();
        var published = new List<string>();
        try
        {
            foreach (var (destination, content) in new[] { (paths.Json, json), (paths.Svg, svg) })
            {
                if (destination is null || content is null) continue;
                string parent = Path.GetDirectoryName(destination) ?? throw new IOException("Missing parent directory.");
                Directory.CreateDirectory(parent);
                string temp = Path.Combine(parent, ".c3-" + Guid.NewGuid().ToString("N") + ".tmp");
                staged.Add((temp, destination));
                await File.WriteAllTextAsync(temp, content, new UTF8Encoding(false));
            }
            // Recheck immediately before publishing; this CLI is not a hostile-filesystem sandbox.
            Preflight(paths.Input, paths.Json, paths.Svg, force);
            foreach (var (temp, destination) in staged)
            {
                File.Move(temp, destination, force);
                published.Add(destination);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException(ex.Message + (published.Count == 0 ? " No outputs published." : " Already published: " + string.Join(", ", published)), ex);
        }
        finally
        {
            foreach (var (temp, _) in staged)
                try { File.Delete(temp); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
