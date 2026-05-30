using Application.Events.Notifications;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Application.Services.Outbox
{
    public class OutboxDispatcher : IOutboxDispatcher
    {
        private const int BatchSize = 50;

        private readonly IOutboxMessageRepository _outboxRepository;
        private readonly IPublisher _publisher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OutboxDispatcher> _logger;

        public OutboxDispatcher(
            IOutboxMessageRepository outboxRepository,
            IPublisher publisher,
            IUnitOfWork unitOfWork,
            ILogger<OutboxDispatcher> logger)
        {
            _outboxRepository = outboxRepository;
            _publisher = publisher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task DispatchPendingAsync(CancellationToken cancellationToken = default)
        {
            var messages = await _outboxRepository.GetPendingAsync(BatchSize, cancellationToken);
            foreach (var message in messages)
            {
                try
                {
                    var notification = Deserialize(message.Type, message.Payload);
                    if (notification == null)
                    {
                        message.MarkFailed($"Unsupported outbox message type '{message.Type}'.");
                    }
                    else
                    {
                        await _publisher.Publish(notification, cancellationToken);
                        message.MarkProcessed();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox dispatch failed for message {MessageId}", message.Id);
                    message.MarkFailed(ex.Message);
                }

                _outboxRepository.Update(message);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        private static INotification? Deserialize(string type, string payload)
        {
            return type switch
            {
                nameof(PaymentConfirmationEvent) => JsonConvert.DeserializeObject<PaymentConfirmationEvent>(payload),
                _ => null
            };
        }
    }
}
