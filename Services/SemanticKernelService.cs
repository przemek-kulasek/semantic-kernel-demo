using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace SemanticKernelDemoWeb.Services;

public class SemanticKernelService
{
    private readonly ILogger<SemanticKernelService> _logger;

    public SemanticKernelService(ILogger<SemanticKernelService> logger)
    {
        _logger = logger;
    }

    public Kernel CreateKernel()
    {
        return Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: "llama3.2",
                apiKey: "not-needed",
                endpoint: new Uri("http://localhost:11434/v1"))
            .Build();
    }

    public async IAsyncEnumerable<string> StreamChatResponseAsync(
        Kernel kernel, 
        string userMessage, 
        ChatHistory history)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        history.AddUserMessage(userMessage);

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.7
        };

        var response = await chatService.GetChatMessageContentAsync(
            history,
            executionSettings,
            kernel);

        history.AddAssistantMessage(response.Content!);
        yield return response.Content!;
    }
}

// Plugins for Demo 1
public class TimePlugin
{
    [KernelFunction, Description("Gets the current date and time")]
    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("F");
    }

    [KernelFunction, Description("Gets the current day of the week")]
    public string GetDayOfWeek()
    {
        return DateTime.Now.DayOfWeek.ToString();
    }
}

public class CalculatorPlugin
{
    [KernelFunction, Description("Performs mathematical calculations on a given expression")]
    public double Calculate([Description("Mathematical expression to evaluate")] string expression)
    {
        try
        {
            var table = new System.Data.DataTable();
            var result = table.Compute(expression, string.Empty);
            return Convert.ToDouble(result);
        }
        catch
        {
            return 0;
        }
    }
}

public class FilePlugin
{
    [KernelFunction, Description("Saves text content to a file")]
    public string SaveToFile(
        [Description("The filename to save to")] string filename,
        [Description("The content to save in the file")] string content)
    {
        try
        {
            File.WriteAllText(filename, content);
            return $"? Successfully saved content to {filename}";
        }
        catch (Exception ex)
        {
            return $"? Error saving file: {ex.Message}";
        }
    }

    [KernelFunction, Description("Reads content from a file")]
    public string ReadFromFile([Description("The filename to read from")] string filename)
    {
        return File.Exists(filename) 
            ? File.ReadAllText(filename) 
            : "? File not found";
    }

    [KernelFunction, Description("Lists all files in the current directory")]
    public string ListFiles()
    {
        var files = Directory.GetFiles(".", "*.txt");
        return files.Length > 0 
            ? string.Join(", ", files.Select(Path.GetFileName))
            : "No text files found";
    }
}

// Plugin for Demo 5
public class ConversationPlugin
{
    private readonly Dictionary<string, string> _userPreferences = new();
    private readonly List<string> _conversationTopics = new();

    [KernelFunction, Description("Saves a user preference or fact for future reference")]
    public string RememberPreference(
        [Description("The key or name of the preference")] string key,
        [Description("The value or details to remember")] string value)
    {
        _userPreferences[key.ToLower()] = value;
        return $"?? I'll remember that your {key} is {value}";
    }

    [KernelFunction, Description("Retrieves a previously saved user preference")]
    public string RecallPreference([Description("The preference key to recall")] string key)
    {
        return _userPreferences.TryGetValue(key.ToLower(), out var value)
            ? $"?? Your {key} is {value}"
            : $"? I don't have any information about {key}";
    }

    [KernelFunction, Description("Tracks topics discussed in the conversation")]
    public string TrackTopic([Description("The topic being discussed")] string topic)
    {
        if (!_conversationTopics.Contains(topic))
        {
            _conversationTopics.Add(topic);
        }
        return $"?? Noted that we're discussing {topic}";
    }

    [KernelFunction, Description("Lists all topics discussed so far")]
    public string GetDiscussedTopics()
    {
        return _conversationTopics.Count > 0
            ? $"?? We've discussed: {string.Join(", ", _conversationTopics)}"
            : "We haven't discussed any specific topics yet";
    }

    [KernelFunction, Description("Generates a fun fact or interesting information")]
    public string GenerateFunFact([Description("Optional topic for the fun fact")] string? topic = null)
    {
        var facts = new[]
        {
            "?? The first computer bug was an actual moth found in a Harvard Mark II computer in 1947",
            "?? Python is named after Monty Python, not the snake",
            "?? The first 1GB hard drive weighed over 500 pounds and cost $40,000",
            "? Git was created in just 10 days by Linus Torvalds"
        };
        return facts[Random.Shared.Next(facts.Length)];
    }
}
