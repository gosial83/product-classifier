using Anthropic;
using Anthropic.Models.Messages;
using ProductClassifier.Models;

namespace ProductClassifier.Services;

/// <summary>
/// Klasyfikuje produkt jednym wywołaniem API. Narzędzie nie jest tu funkcją,
/// którą model wywołuje po dane — to sposób na wymuszenie ustrukturyzowanej
/// odpowiedzi, więc nie odsyłamy tool_result i nie ma pętli agentowej.
/// </summary>
public class ClaudeProductClassifier
{
    public const string ModelId = "claude-haiku-5-5";

    private readonly AnthropicClient _client;

    public ClaudeProductClassifier(AnthropicClient client) => _client = client;

    /// <exception cref="ClassificationException"/>
    public async Task<Classification> ClassifyAsync(Product product)
    {
        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = ModelId,
            MaxTokens = 1024,
            System = ClassificationPrompt.System,
            Tools = [ClassificationTool.Definition],
            // Wymuszone narzędzie: model nie może odpowiedzieć zwykłym tekstem.
            // Skutek uboczny — pomija thinking i od razu wywołuje narzędzie.
            ToolChoice = new ToolChoiceTool { Name = ClassificationTool.Name },
            Messages =
            [
                new() { Role = Role.User, Content = ClassificationPrompt.BuildUserMessage(product) },
            ],
        });

        if (response.StopReason == "refusal")
        {
            var powod = response.StopDetails?.Explanation ?? "brak szczegółów";
            throw new ClassificationException($"Model odmówił odpowiedzi: {powod}");
        }

        if (response.StopReason == "max_tokens")
        {
            throw new ClassificationException("Odpowiedź została ucięta na limicie max_tokens.");
        }

        foreach (var block in response.Content)
        {
            if (block.TryPickToolUse(out var toolUse) && toolUse.Name == ClassificationTool.Name)
            {
                return ClassificationTool.Parse(toolUse.Input);
            }
        }

        throw new ClassificationException(
            $"Model nie wywołał narzędzia {ClassificationTool.Name} (stop_reason: {response.StopReason}).");
    }
}
