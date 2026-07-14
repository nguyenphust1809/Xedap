namespace Xedap.Services.Llm
{
    public class OllamaChatProvider: ILlmChatProvider
    {
        public Task<string> AskAsync(string system, string user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
