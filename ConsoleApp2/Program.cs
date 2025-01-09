using System.CommandLine;

var rootCommand = new RootCommand("Manage code bundling");

var bundleCommand = new Command("bundle", "Bundle code files into a single output file");

var languageOption = new Option<List<string>>(
    aliases: new[] { "--language", "-l" },
    description: "List of programming languages to include. Use 'all' to include all files.")
{
    IsRequired = true
};

bundleCommand.AddOption(languageOption);

var outputOption = new Option<FileInfo>(
    aliases: new[] { "--output", "-o" },
    description: "Output file path (optional). If not provided, the file will be created in the current directory.");
bundleCommand.AddOption(outputOption);

var noteOption = new Option<bool>(
    aliases: new[] { "--note", "-n" },
    description: "Include a comment with the source file's path.");
bundleCommand.AddOption(noteOption);

var sortOption = new Option<string>(
    aliases: new[] { "--sort", "-s" },
    getDefaultValue: () => "name",
    description: "Sort files by 'name' or 'type'. Default is 'name'.");
bundleCommand.AddOption(sortOption);

var removeEmptyLinesOption = new Option<bool>(
    aliases: new[] { "--remove-empty-lines", "-r" },
    description: "Remove empty lines from source code.");
bundleCommand.AddOption(removeEmptyLinesOption);

var authorOption = new Option<string>(
    aliases: new[] { "--author", "-a" },
    description: "Author name to include as a comment in the output file.");
bundleCommand.AddOption(authorOption);



rootCommand.Add(bundleCommand);

bool FileExistsAndConfirm(string filePath)
{
    if (File.Exists(filePath))
    {
        Console.WriteLine($"The file '{filePath}' already exists. Do you want to overwrite it? (yes/no):");
        var response = Console.ReadLine()?.Trim().ToLower();
        return response == "yes";
    }
    return true;
}

bool IsValidLanguageInput(List<string> language)
{
    var validLanguages = new List<string> { "csharp", "java", "python", "javascript", "typescript", "php", "cpp", "go", "ruby", "html", "css", "all" };
    return language.All(lang => validLanguages.Contains(lang.ToLower()));
}

bool IsValidFilePath(string path)
{
    return !string.IsNullOrWhiteSpace(path) && Path.GetExtension(path) != "";
}

bool IsValidSortOption(string sort)
{
    return sort.ToLower() == "name" || sort.ToLower() == "type";
}

bool IsValidRemoveEmptyLinesOption(string remove)
{
    return remove.ToLower() == "yes" || remove.ToLower() == "no";
}

bool IsValidNoteOption(string note)
{
    return note.ToLower() == "yes" || note.ToLower() == "no";
}

bool IsValidLanguageAddition(string input)

{
    if (string.IsNullOrWhiteSpace(input)) return false;
    var parts = input.Split(':');
    return parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && parts[1].Trim().StartsWith(".");
}

bundleCommand.SetHandler((List<string> language, FileInfo output, bool note, string sort, bool removeEmptyLines, string author) =>
{
    if (!IsValidLanguageInput(language))
    {
        Console.WriteLine("Error: Invalid language input. Supported values: csharp, java, python, javascript, typescript, php, cpp, go, ruby, html, css, all.");
        return;
    }

    if (output != null && (!IsValidFilePath(output.FullName) || !FileExistsAndConfirm(output.FullName)))
    {
        Console.WriteLine("Error: Invalid output file path or operation canceled.");
        return;
    }

    if (!IsValidSortOption(sort))
    {
        Console.WriteLine("Error: Invalid sort option. Use 'name' or 'type'.");
        return;
    }
    try
    {
        if (output == null)
        {
            output = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "bundle_output.txt"));
        }

        var files = Directory.GetFiles(Directory.GetCurrentDirectory());

        var excludedDirectories = new List<string> { "bin", "debug", "node_modules", "properties" };

        var selectedFiles = files.Where(file =>
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            var directory = Path.GetDirectoryName(file)?.ToLowerInvariant();

            if (directory != null && excludedDirectories.Any(excluded => directory.Contains(excluded)))
            {
                Console.WriteLine($"Excluding file: {file} (from excluded directory)");
                return false;
            }

            if (language.Contains("all"))
                return true;

            var languageExtensions = new Dictionary<string, string>
            {
                { ".cs", "csharp" },
                { ".java", "java" },
                { ".py", "python" },
                { ".js", "javascript" },
                { ".ts", "typescript" },
                { ".php", "php" },
                { ".cpp", "cpp" },
                { ".go", "go" },
                { ".rb", "ruby" },
                { ".html", "html" },
                { ".css", "css" },
            };

            Console.WriteLine($"Checking file: {file} with extension {extension}");

            return languageExtensions.TryGetValue(extension.ToString(), out var lang) && language.Contains(lang);
        });

        selectedFiles = sort switch
        {
            "type" => selectedFiles.OrderBy(file => Path.GetExtension(file)).ThenBy(file => Path.GetFileName(file)),
            "name" => selectedFiles.OrderBy(file => Path.GetFileName(file)),
            _ => selectedFiles.OrderBy(file => Path.GetFileName(file))
        };

        using (var writer = new StreamWriter(output.FullName))
        {
            foreach (var file in selectedFiles)
            {
                var lines = File.ReadAllLines(file);

                if (removeEmptyLines)
                    lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                if (!string.IsNullOrWhiteSpace(author))
                {
                    writer.WriteLine($"// Author: {author}");
                    writer.WriteLine();
                }
                if (note)
                    writer.WriteLine($"// File: {Path.GetFileName(file)} - Path: {file}");
                else
                    writer.WriteLine($"// File: {Path.GetFileName(file)}");

                writer.WriteLine(string.Join(Environment.NewLine, lines));
                writer.WriteLine();
            }
            Console.WriteLine($"Bundle created successfully: {output.FullName}");
        }

    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("Error: file path invalid");
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine("Error: Access denied to the specified path.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
    }
}, languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

var createRspCommand = new Command("create-rsp", "Create a response file for bundling files");

rootCommand.Add(createRspCommand);

createRspCommand.SetHandler(() =>
{
    try
    {
        Console.WriteLine("Select the programming languages to include (e.g., csharp, python, or all):");
        var languageInput = Console.ReadLine();

        Console.WriteLine("Where should the output file be saved? (Press Enter to use the default: bundle_output.txt):");
        var outputPath = Console.ReadLine();

        Console.WriteLine("Would you like to add a comment showing the file's original path? (yes/no) (Default: yes):");
        var includeNote = Console.ReadLine()?.Trim().ToLower() == "yes";

        Console.WriteLine("How should the files be sorted? Choose 'name' or 'type':");
        var sortOption = Console.ReadLine();

        Console.WriteLine("Should empty lines be removed from the code? (yes/no) (Default: yes):");
        var removeEmptyLines = Console.ReadLine()?.Trim().ToLower() == "yes";

        Console.WriteLine("Enter the name of the author (leave blank if not applicable):");
        var authorName = Console.ReadLine();

        //Create the full command
        var rspCommand = $"bundle -l {languageInput}";

        if (!string.IsNullOrWhiteSpace(outputPath))
            rspCommand += $" -o {outputPath}";
        if (!string.IsNullOrWhiteSpace(authorName))
            rspCommand += $" -a {authorName}";
        if (includeNote)
            rspCommand += $" -n";
        if (removeEmptyLines)
            rspCommand += $" -r";

        rspCommand += $" -s {sortOption}";

        var responseFilePath = "command.rsp";
        File.WriteAllText(responseFilePath, rspCommand);

        Console.WriteLine($"Response file created: {responseFilePath}");
        Console.WriteLine($"Run the command using: fib @{responseFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }

});

rootCommand.InvokeAsync(args);