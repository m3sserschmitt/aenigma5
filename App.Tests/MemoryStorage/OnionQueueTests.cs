using Enigma5.App.MemoryStorage;
using FluentAssertions;

namespace App.Tests;

public class OnionQueueTests
{
    [Fact]
    public void OnionQueue_ShouldAddItem()
    {
        // Arrange
        var onionQueue = new OnionQueue();
        
        // Act
        onionQueue.Add(new OnionQueueItem());
        onionQueue.Add(new OnionQueueItem());
        onionQueue.Add(new OnionQueueItem());

        // Assert
        onionQueue.Count.Should().Be(3);
    }

    [Fact]
    public void OnionQueue_ShouldRetrieveCorrectItems()
    {
        // Arrange
        var onionQueue = new OnionQueue();
        onionQueue.Add(new OnionQueueItem { Destination = "destination-1" });
        onionQueue.Add(new OnionQueueItem { Destination = "destination-1" });
        onionQueue.Add(new OnionQueueItem { Destination = "destination-2" });

        // Act
        var items = onionQueue.Where(item => item.Destination == "destination-1");

        // Assert
        items.Should().NotBeNullOrEmpty();
        foreach(var item in items)
        {
            item.Destination.Should().Be("destination-1");
        }
    }

    [Fact]
    public void OnionQueue_ShouldRemoveCorrectItemsWhenDeadlineProvided()
    {
        // Arrange
        var currentTime = DateTime.Now;
        var timeSpan = new TimeSpan(24, 0, 0);
        var onionQueue = new OnionQueue();
        onionQueue.Add(new OnionQueueItem { Destination = "destination-1", DateReceived = currentTime });
        onionQueue.Add(new OnionQueueItem { Destination = "destination-1", DateReceived = currentTime.AddMinutes(-4) });
        onionQueue.Add(new OnionQueueItem { Destination = "destination-2", DateReceived = currentTime.AddDays(-1.1) });

        // Act
        onionQueue.Cleanup(timeSpan);

        // Assert
        onionQueue.Count.Should().Be(2);
        foreach(var item in onionQueue.Where(item => true))
        {
            (currentTime - item.DateReceived).Should().BeLessThan(timeSpan);
            item.Destination.Should().Be("destination-1");
        }
    }

    [Fact]
    public void OnionQueue_ShouldRemoveCorrectItemsWhenConditionProvided()
    {
        // Arrange
        var onionQueue = new OnionQueue();
        onionQueue.Add(new OnionQueueItem { Destination = "destination-1" });
        onionQueue.Add(new OnionQueueItem { Destination = "destination-1" });
        onionQueue.Add(new OnionQueueItem { Destination = "destination-2" });

        // Act
        onionQueue.Cleanup(item => item.Destination == "destination-1");

        // Assert
        onionQueue.Count.Should().Be(1);
        foreach(var item in onionQueue.Where(item => true))
        {
            item.Destination.Should().Be("destination-2");
        }
    }
}
