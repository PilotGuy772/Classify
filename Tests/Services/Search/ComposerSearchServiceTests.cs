using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Repository;
using Classify.Core.Interfaces.Service;
using Classify.Data.Services;
using FluentAssertions;
using Moq;

namespace Tests.Services.Search;

public class ComposerSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenQueryIsNullOrWhitespace()
    {
        // Arrange
        Mock<IComposerRepository> mockRepo = new();
        Mock<IUnitOfWork> mockUow = new();
        mockUow.SetupGet(u => u.Composers).Returns(mockRepo.Object);

        ComposerSearchService svc = new(mockUow.Object);

        // Act
        IEnumerable<SearchResult> empty1 = await svc.SearchAsync("", CancellationToken.None);
        IEnumerable<SearchResult> empty2 = await svc.SearchAsync("   ", CancellationToken.None);
        IEnumerable<SearchResult> empty3 = await svc.SearchAsync(null!, CancellationToken.None);

        // Assert
        empty1.Should().BeEmpty();
        empty2.Should().BeEmpty();
        empty3.Should().BeEmpty();
        mockRepo.Verify(r => r.FindByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_CallsRepositoryAndMapsResults()
    {
        // Arrange
        List<Composer> composers =
        [
            new() { Id = 1, Name = "Beethoven" },
            new() { Id = 2, Name = "Bach" }
        ];

        Mock<IComposerRepository> mockRepo = new();
        mockRepo.Setup(r => r.FindByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string q, int limit, CancellationToken ct) => composers.Where(c => c.Name.Contains(q)).ToList());

        Mock<IUnitOfWork> mockUow = new();
        mockUow.SetupGet(u => u.Composers).Returns(mockRepo.Object);

        ComposerSearchService svc = new(mockUow.Object);

        // Act
        List<SearchResult> results = (await svc.SearchAsync("B", CancellationToken.None)).ToList();

        // Assert
        results.Should().NotBeNull();
        results.Select(r => r.DisplayText).Should().Contain(["Beethoven", "Bach"]);
        results.Select(r => r.Entity).Should().Contain(composers);

        mockRepo.Verify(r => r.FindByNameAsync("B", 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_PassesCancellationTokenToRepository()
    {
        // Arrange
        CancellationToken? captured = null;
        Mock<IComposerRepository> mockRepo = new();
        mockRepo.Setup(r => r.FindByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, CancellationToken>((q, l, ct) => captured = ct)
            .ReturnsAsync(new List<Composer>());

        Mock<IUnitOfWork> mockUow = new();
        mockUow.SetupGet(u => u.Composers).Returns(mockRepo.Object);

        ComposerSearchService svc = new(mockUow.Object);

        using CancellationTokenSource cts = new();

        // Act
        await svc.SearchAsync("Beet", cts.Token);

        // Assert
        captured.HasValue.Should().BeTrue();
        captured!.Value.Should().Be(cts.Token);
    }
}

