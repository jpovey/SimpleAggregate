namespace SimpleAggregate.UnitTests
{
    using System;
    using System.Collections.Generic;
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
        private Mock<IEventStore> _eventStoreMock;
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

            _eventStoreMock = new Mock<IEventStore>();
            _eventStoreMock.Setup(x => x.LoadAsync(_aggregateId)).ReturnsAsync(default(IEnumerable<object>));
            _sut = new AggregateProcessor(_eventStoreMock.Object);
        }

        [Test]
        public async Task NotRehydrateAggregateUsingLoadedEvents_GivenEventsDoNotExist()
        {
            await _sut.ProcessAsync(_aggregate, null);

            _eventStoreMock.Verify(x => x.LoadAsync(_aggregateId), Times.Once);
            _aggregate.Balance.Should().Be(default(decimal));
        }

        [Test]
        public async Task HydrateAggregateUsingLoadedEvents_GivenEventsExist()
        {
            _eventStoreMock.Setup(x => x.LoadAsync(_aggregateId)).ReturnsAsync(new List<object>
            {
                new AccountCredited { Amount = _creditAmount}
            });

            await _sut.ProcessAsync(_aggregate, null);

            _eventStoreMock.Verify(x => x.LoadAsync(_aggregateId), Times.Once);
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

            _eventStoreMock.Verify(x => x.SaveAsync(_aggregateId, It.Is<IEnumerable<object>>(y => VerifyUncommittedEvents(y, expectedEvents))), Times.Once);
        }

        [Test]
        public async Task NotSaveUncommittedEvents_GivenAggregateDoesNotHaveUncommittedEvents()
        {
            await _sut.ProcessAsync(_aggregate, bankAccount => bankAccount.DoNothing());

            _eventStoreMock.Verify(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<IEnumerable<object>>()), Times.Never);
        }

        private static bool VerifyUncommittedEvents(IEnumerable<object> events, IEnumerable<object> expectedEvents)
        {
            events.Should().BeEquivalentTo(expectedEvents);
            return true;
        }
    }
}
