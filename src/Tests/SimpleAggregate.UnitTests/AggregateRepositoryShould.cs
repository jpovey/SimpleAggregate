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

    [TestFixture]
    public class AggregateRepositoryShould
    {
        private readonly Fixture _fixture = new Fixture();
        private AggregateRepository<BankAccount> _sut;
        private Mock<IEventStream> _eventStreamMock;
        private string _aggregateId;
        private CancellationToken _cancellationToken;
        private decimal _expectedAccountBalance;
        private List<object> _committedEvents;
        private EventStreamContext _eventStreamContext;

        [SetUp]
        public void SetUp()
        {
            _aggregateId = _fixture.Create<string>();
            _cancellationToken = new CancellationToken();
            _eventStreamMock = new Mock<IEventStream>();

            var creditAmount1 = _fixture.Create<decimal>();
            var creditAmount2 = _fixture.Create<decimal>();
            var debitAmount = _fixture.Create<decimal>();
            _expectedAccountBalance = creditAmount1 + creditAmount2 - debitAmount;

            _committedEvents = new List<object>{
                new AccountCreated { AccountId = _aggregateId },
                new AccountCredited { Amount = creditAmount1 },
                new AccountDebited { Amount = debitAmount },
                new AccountCredited { Amount = creditAmount2 }
            };

            _eventStreamContext = new EventStreamContext(_committedEvents, _fixture.Create<string>());
            _eventStreamMock.Setup(x => x.ReadAsync(_aggregateId, _cancellationToken)).ReturnsAsync(_eventStreamContext);

            _sut = new AggregateRepository<BankAccount>(_eventStreamMock.Object);
        }

        [Test]
        public async Task ReturnNewAggregate_WhenGettingAggregate()
        {
            var result = await _sut.GetAsync(_aggregateId, _cancellationToken);

            result.Should().NotBeNull();
            result.Should().BeOfType<BankAccount>();
        }

        [Test]
        public async Task ReadEventStream_WhenGettingAggregate()
        {
            await _sut.GetAsync(_aggregateId, CancellationToken.None);

            _eventStreamMock.Verify(x => x.ReadAsync(_aggregateId, _cancellationToken), Times.Once);
        }

        [Test]
        public async Task RehydrateAggregate_WhenGettingAggregate()
        {
            var result = await _sut.GetAsync(_aggregateId, CancellationToken.None);

            result.Balance.Should().Be(_expectedAccountBalance);
        }

        [Test]
        public async Task NotThrowException_WhenGettingAggregate_GivenEventStreamIsNull()
        {
            var eventStreamContext = new EventStreamContext(null, _fixture.Create<string>());
            _eventStreamMock.Setup(x => x.ReadAsync(_aggregateId, _cancellationToken)).ReturnsAsync(eventStreamContext);

            Func<Task> act = async () => await _sut.GetAsync(_aggregateId, CancellationToken.None);

            await act.Should().NotThrowAsync<Exception>();
        }

        [Test]
        public async Task NotThrowException_WhenGettingAggregate_GivenEventStreamIsEmpty()
        {
            var eventStreamContext = new EventStreamContext(new List<object>(), _fixture.Create<string>());
            _eventStreamMock.Setup(x => x.ReadAsync(_aggregateId, _cancellationToken)).ReturnsAsync(eventStreamContext);

            Func<Task> act = async () => await _sut.GetAsync(_aggregateId, CancellationToken.None);

            await act.Should().NotThrowAsync<Exception>();
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public async Task ThrowArgumentException_WhenGettingAggregate_GivenAggregateIdIsInvalid(string aggregateId)
        {
            Func<Task> act = async () => await _sut.GetAsync(aggregateId, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Test]
        public async Task UpdateAggregateConcurrencyKey_WhenGettingAggregate()
        {
            var result = await _sut.GetAsync(_aggregateId, CancellationToken.None);

            result.ConcurrencyKey.Should().Be(_eventStreamContext.ConcurrencyKey);
        }

        [Test]
        public async Task AppendUncommittedEventsToStream_WhenSavingAggregate()
        {
            var aggregate = new BankAccount(_aggregateId);
            aggregate.CreditAccount(_fixture.Create<decimal>());
            aggregate.DebitAccount(_fixture.Create<decimal>());
            aggregate.CreditAccount(_fixture.Create<decimal>());

            await _sut.SaveAsync(aggregate, _cancellationToken);

            _eventStreamMock.Verify(x => x.AppendAsync(_aggregateId, aggregate.UncommittedEvents, It.IsAny<object>(), _cancellationToken), Times.Once);
        }

        [Test]
        public async Task NotAppendEventsToStream_WhenSavingAggregate_GivenUncommittedEventsIsEmpty()
        {
            var aggregate = new BankAccount();
            var events = new List<object>
            {
                new AccountCreated{ AccountId = _aggregateId }
            };
            aggregate.Rehydrate(events);

            await _sut.SaveAsync(aggregate, _cancellationToken);

            _eventStreamMock.Verify(x => x.AppendAsync(It.IsAny<string>(), It.IsAny<IEnumerable<object>>(), It.IsAny<object>(), _cancellationToken), Times.Never);
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public async Task ThrowArgumentException_WhenSavingAggregate_GivenAggregateIdIsInvalid(string aggregateId)
        {
            var aggregate = new BankAccount(aggregateId);

            Func<Task> act = async () => await _sut.SaveAsync(aggregate, _cancellationToken);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Test]
        public async Task AppendToStreamWithUsingAggregateConcurrencyKey_WhenSavingAggregate_GivenStreamWasNotLoaded()
        {
            var aggregate = new BankAccount(_aggregateId) { ConcurrencyKey = _fixture.Create<string>() };

            await _sut.SaveAsync(aggregate, _cancellationToken);

            _eventStreamMock.Verify(x => x.AppendAsync(_aggregateId, aggregate.UncommittedEvents, aggregate.ConcurrencyKey, _cancellationToken), Times.Once);
        }

        [Test]
        public async Task AppendToStreamWithLoadedConcurrencyKey_WhenSavingAggregate_GivenStreamWasLoaded()
        {
            var aggregate = await _sut.GetAsync(_aggregateId, CancellationToken.None);
            aggregate.CreditAccount(_fixture.Create<decimal>());

            await _sut.SaveAsync(aggregate, _cancellationToken);

            _eventStreamMock.Verify(x => x.AppendAsync(_aggregateId, aggregate.UncommittedEvents, _eventStreamContext.ConcurrencyKey, _cancellationToken), Times.Once);
        }
    }
}
