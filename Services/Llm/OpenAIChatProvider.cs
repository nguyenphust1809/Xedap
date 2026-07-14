using OpenAI.Chat;

namespace Xedap.Services.Llm
{
    public class OpenAIChatProvider: ILlmChatProvider
    {
        private readonly ChatClient _chatClient;
        public OpenAIChatProvider(ChatClient chatClient)
        {
            _chatClient = chatClient;
        }
        public async Task<string> AskAsync(string system, string user, CancellationToken cancellationToken= default)
        {
            var messages = new List<ChatMessage>
            { new SystemChatMessage(system),
            new UserChatMessage(user)
            };
            var completion = await _chatClient.CompleteChatAsync(
                messages: messages,
                cancellationToken: cancellationToken);
                return completion.Value.Content.Count > 0 ?
                    completion.Value.Content[0].Text.ToString() : string.Empty;
        }


    }
}
