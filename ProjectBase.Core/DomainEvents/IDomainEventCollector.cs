using MediatR;

namespace ProjectBase.Core.DomainEvents;

/// <summary>
/// Handler'lardan toplanan notification'ları saklar.
/// Transaction commit sonrası dispatch edilmek üzere kullanılır.
/// </summary>
public interface IDomainEventCollector
{
    void AddNotification(INotification notification);
    IReadOnlyList<INotification> GetNotifications();
    void Clear();
}

