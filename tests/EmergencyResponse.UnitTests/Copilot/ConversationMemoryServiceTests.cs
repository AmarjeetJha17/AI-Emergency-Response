using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Domain.Copilot;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Copilot;
using EmergencyResponse.UnitTests.Infrastructure;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.UnitTests.Copilot;

public sealed class ConversationMemoryServiceTests
{
    [Fact]
    public async Task AddMessageAsync_UsesAlreadyTrackedConversation()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var service = new ConversationMemoryService(
            fixture.Context,
            Options.Create(new CopilotMemoryOptions()));

        var conversation = await service.GetOrCreateAsync("operator-1", null, null);
        var previousActivity = conversation.LastActivityAtUtc;
        var message = new CopilotMessage
        {
            Role = CopilotMessageRole.User,
            Content = "What procedure applies to a chemical spill?"
        };

        await service.AddMessageAsync("operator-1", conversation.Id, message);

        Assert.Equal(1, message.SequenceNumber);
        Assert.Equal(conversation.Id, message.CopilotConversationId);
        Assert.True(conversation.LastActivityAtUtc >= previousActivity);
        Assert.Equal(1, fixture.Context.CopilotMessages.Count());
    }
}
