namespace SimpleAggregate.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Domain;
    using Domain.Events;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;

    public class AggregateProcessorShould
    {
        private readonly Fixture _fixture = new Fixture();
        private Mock<IEventSource> _eventSourceMock;
        private string _aggregateId;
        private string _concurrencyKey;
        private decimal _creditAmount;
        private BankAccount _aggregate;
        private AggregateProcessor _sut;
        private EventSourceResult _eventSourceResult;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Setup()
        {
            _cancellationToken = new CancellationToken();
            _aggregateId = _fixture.Create<string>();
            _concurrencyKey = _fixture.Create<string>();
            _creditAmount = _fixture.Create<decimal>();
            _aggregate = new BankAccount(_aggregateId);

            _eventSourceMock = new Mock<IEventSource>();
            var events = new List<object> { new AccountCredited { Amount = _creditAmount } };
            _eventSourceResult = new EventSourceResult(events, _concurrencyKey);
            _eventSourceMock.Setup(x => x.ReadEventsAsync(_aggregateId, CancellationToken.None)).ReturnsAsync(_eventSourceResult);

            _sut = new AggregateProcessor(_eventSourceMock.Object);
        }

        [Test]
        public async Task ReadEvents()
        {
            await _sut.ProcessAsync(_aggregate, null, _cancellationToken);

            _eventSourceMock.Verify(x => x.ReadEventsAsync(_aggregateId, CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task NotRehydrateAggregateUsingLoadedEvents_GivenEventListIsNull()
        {
            _eventSourceMock.Setup(x => x.ReadEventsAsync(_aggregateId, CancellationToken.None)).ReturnsAsync(new EventSourceResult(default(IEnumerable<object>), _concurrencyKey));

            await _sut.ProcessAsync(_aggregate, async bankAccount => await bankAccount.CreditAccount(_creditAmount), _cancellationToken);

            _aggregate.Balance.Should().Be(_creditAmount);
        }

        [Test]
        public async Task NotRehydrateAggregateUsingLoadedEvents_GivenEventsListIsEmpty()
        {
            _eventSourceMock.Setup(x => x.ReadEventsAsync(_aggregateId, CancellationToken.None)).ReturnsAsync(new EventSourceResult(new List<object>(), _concurrencyKey));

            await _sut.ProcessAsync(_aggregate, async bankAccount => await bankAccount.CreditAccount(_creditAmount), _cancellationToken);

            _aggregate.Balance.Should().Be(_creditAmount);
        }

        [Test]
        public async Task HydrateAggregateUsingLoadedEvents_GivenEventsExist()
        {
            _eventSourceMock.Setup(x => x.ReadEventsAsync(_aggregateId, CancellationToken.None)).ReturnsAsync(_eventSourceResult);

            await _sut.ProcessAsync(_aggregate, null, _cancellationToken);

            _aggregate.Balance.Should().Be(_creditAmount);
        }

        [Test]
        public async Task CallAggregateAction()
        {
            var newCreditAmount = _fixture.Create<decimal>();
            var expectedCreditAccount = newCreditAmount + _creditAmount;

            await _sut.ProcessAsync(_aggregate, async bankAccount => await bankAccount.CreditAccount(newCreditAmount), _cancellationToken);

            _aggregate.Balance.Should().Be(expectedCreditAccount);
        }

        [Test]
        public void NotThrowException_GivenActionIsNull()
        {
            _eventSourceMock.Setup(x => x.ReadEventsAsync(_aggregateId, CancellationToken.None)).ReturnsAsync(new EventSourceResult(default(IEnumerable<object>), _concurrencyKey));

            Func<Task> act = async () => await _sut.ProcessAsync(_aggregate, null, _cancellationToken);

            act.Should().NotThrow<Exception>();

            _aggregate.Balance.Should().Be(default(decimal));
        }

        [Test]
        public async Task SaveUncommittedEvents_GivenAggregateDoesHaveUncommittedEvents()
        {
            var expectedEvents = new List<object>
            {
                new AccountCredited { Amount = _creditAmount}
            };

            await _sut.ProcessAsync(_aggregate, async bankAccount => await bankAccount.CreditAccount(_creditAmount), _cancellationToken);

            _eventSourceMock.Verify(x => x.CommitEventsAsync(_aggregateId, It.Is<IEnumerable<object>>(y => VerifyUncommittedEvents(y, expectedEvents)), It.IsAny<object>(), _cancellationToken), Times.Once);
        }

        [Test]
        public async Task NotSaveUncommittedEvents_GivenAggregateDoesNotHaveUncommittedEvents()
        {
            await _sut.ProcessAsync(_aggregate, bankAccount => bankAccount.DoNothing(), _cancellationToken);

            _eventSourceMock.Verify(x => x.CommitEventsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<object>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SaveEventsUsingConcurrencyKey()
        {
            await _sut.ProcessAsync(_aggregate, async bankAccount => await bankAccount.CreditAccount(_creditAmount), _cancellationToken);

            _eventSourceMock.Verify(x => x.CommitEventsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<object>>(), _eventSourceResult.ConcurrencyKey, _cancellationToken), Times.Once);
        }

        private static bool VerifyUncommittedEvents(IEnumerable<object> events, IEnumerable<object> expectedEvents)
        {
            events.Should().BeEquivalentTo(expectedEvents);
            return true;
        }
    }
}
