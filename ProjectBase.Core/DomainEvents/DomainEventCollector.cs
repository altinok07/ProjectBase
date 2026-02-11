using MediatR;

namespace ProjectBase.Core.DomainEvents;

/// <summary>
/// Scope bazlı notification collector.
/// Her HTTP request için ayrı instance oluşturulur.
/// </summary>
public class DomainEventCollector : IDomainEventCollector
{
    private readonly List<INotification> _notifications = [];

    public void AddNotification(INotification notification)
    {
        _notifications.Add(notification);
    }

    public IReadOnlyList<INotification> GetNotifications()
    {
        return _notifications.AsReadOnly();
    }

    public void Clear()
    {
        _notifications.Clear();
    }
}

