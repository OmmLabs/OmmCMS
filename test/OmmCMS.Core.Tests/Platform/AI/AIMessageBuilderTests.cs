using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using OmmCMS.Core.AI;
using OmmCMS.Core.AI.Prompting;

namespace OmmCMS.Core.Tests.Platform.AI;

[TestFixture]
public class AIMessageBuilderTests
{
    private sealed class TestResources() : AIMessageResources(null)
    {
        public override string GetResource(string key, params object[] args)
            => key + (args.Length > 0 ? ":" + string.Join(",", args) : "");
    }

    [TestCase(6)]
    [TestCase(7)]
    [TestCase(10)]
    public void RejectsRemovedOrUnknownAiRole(int role)
    {
        var resources = new TestResources();
        Assert.Throws<ArgumentOutOfRangeException>(() => resources.Role((AIRole)role));
    }

    [TestCase(AIRole.HtmlEditor, 8)]
    [TestCase(AIRole.ImageAnalyzer, 9)]
    public void RetainsExistingEditorRoleIdentifiers(AIRole role, int expected)
    {
        Assert.That((int)role, Is.EqualTo(expected));
        Assert.That(new TestResources().Role(role), Does.StartWith($"OmmCMS.AI.Prompts.Role.{role}"));
    }

    [TestCase("Topic", "Body")]
    [TestCase("BlogPost", "FullDescription")]
    public async Task NamedCmsContentReceivesTitleInstruction(string type, string property)
    {
        var model = new Mock<IAITextModel>();
        model.SetupGet(x => x.EntityName).Returns("About us");
        model.SetupGet(x => x.Type).Returns(type);
        model.SetupGet(x => x.TargetProperty).Returns(property);
        var builder = new AIMessageBuilder(null, null, null, null, new TestResources());

        var chat = await builder.AddTextMessagesAsync(model.Object, new AIChat(AIChatTopic.RichText));

        Assert.That(chat.Messages.Select(x => x.Content), Does.Contain("OmmCMS.AI.Prompts.DontCreateTitle:About us"));
    }
}
