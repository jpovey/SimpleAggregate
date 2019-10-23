namespace SimpleAggregate.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        private decimal _creditAmount;
        private BankAccount _aggregate;
        private AggregateProcessor _sut;

        [SetUp]
        public void Setup()
        {
            _aggregateId = _fixture.Create<string>();
            _creditAmount = _fixture.Create<decimal>();
            _aggregate = new BankAccount(_aggregateId);

            _eventSourceMock = new Mock<IEventSource>();
            _eventSourceMock.Setup(x => x.LoadEventsAsync(_aggregateId)).ReturnsAsync(default(IEnumerable<object>));
            _sut = new AggregateProcessor(_eventSourceMock.Object);
        }

        [Test]
        public async Task NotRehydrateAggregateUsingLoadedEvents_GivenEventsDoNotExist()
        {
            await _sut.ProcessAsync(_aggregate, null);

            _eventSourceMock.Verify(x => x.LoadEventsAsync(_aggregateId), Times.Once);
            _aggregate.Balance.Should().Be(default(decimal));
        }

        [Test]
        public async Task HydrateAggregateUsingLoadedEvents_GivenEventsExist()
        {
            _eventSourceMock.Setup(x => x.LoadEventsAsync(_aggregateId)).ReturnsAsync(new List<object>
            {
                new AccountCredited { Amount = _creditAmount}
            });

            await _sut.ProcessAsync(_aggregate, null);

            _eventSourceMock.Verify(x => x.LoadEventsAsync(_aggregateId), Times.Once);
            _aggregate.Balance.Should().Be(_creditAmount);
        }

        [Test]
        public async Task CallAggregateAction()
        {
            await _sut.ProcessAsync(_aggregate, async bankAccount => await bankAccount.CreditAccount(_creditAmount));

            _aggregate.Balance.Should().Be(_creditAmount);
        }

        [Test]
        public void NotThrowException_GivenActionIsNull()
        {
            Func<Task> act = async () => await _sut.ProcessAsync(_aggregate, null);

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

            await _sut.ProcessAsync(_aggregate, async bankAccount => await bankAccount.CreditAccount(_creditAmount));

            _eventSourceMock.Verify(x => x.SaveEventsAsync(_aggregateId, It.Is<IEnumerable<object>>(y => VerifyUncommittedEvents(y, expectedEvents)), 0), Times.Once);
        }

        [Test]
        public async Task NotSaveUncommittedEvents_GivenAggregateDoesNotHaveUncommittedEvents()
        {
            await _sut.ProcessAsync(_aggregate, bankAccount => bankAccount.DoNothing());

            _eventSourceMock.Verify(x => x.SaveEventsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<object>>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task SetExpectedEventCountEqualToCommittedEventCount_WhenSavingUncommittedEvents()
        {
            var rnd = new Random();
            var expectedEventCount = rnd.Next(1, 10);
            var committedEvents = _fixture.CreateMany<AccountCredited>(expectedEventCount).ToList();
            _eventSourceMock.Setup(x => x.LoadEventsAsync(_aggregateId)).ReturnsAsync(committedEvents);

            await _sut.ProcessAsync(_aggregate, async bankAccount => await bankAccount.CreditAccount(_creditAmount));

            _eventSourceMock.Verify(x => x.SaveEventsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<object>>(), expectedEventCount), Times.Once);
        }

        private static bool VerifyUncommittedEvents(IEnumerable<object> events, IEnumerable<object> expectedEvents)
        {
            events.Should().BeEquivalentTo(expectedEvents);
            return true;
        }
    }
}
