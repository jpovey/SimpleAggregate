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
            _eventStoreMock.Setup(x => x.LoadEvents(_aggregateId)).ReturnsAsync(default(IEnumerable<object>));
            _sut = new AggregateProcessor(_eventStoreMock.Object);
        }

        [Test]
        public async Task NotHydrateAggregate_GivenEventsDoNotExist()
        {
            await _sut.Process(_aggregate, null);

            _eventStoreMock.Verify(x => x.LoadEvents(_aggregateId), Times.Once);
            _aggregate.Balance.Should().Be(default(decimal));
        }

        [Test]
        public async Task HydrateAggregate_GivenEventsExist()
        {
            _eventStoreMock.Setup(x => x.LoadEvents(_aggregateId)).ReturnsAsync(new List<object>
            {
                new AccountCredited { Amount = _creditAmount}
            });

            await _sut.Process(_aggregate, null);

            _eventStoreMock.Verify(x => x.LoadEvents(_aggregateId), Times.Once);
            _aggregate.Balance.Should().Be(_creditAmount);
        }

        [Test]
        public async Task CallAggregateAction()
        {
            await _sut.Process(_aggregate, async order => await order.CreditAccount(_creditAmount));

            _aggregate.Balance.Should().Be(_creditAmount);
        }

        [Test]
        public void NotThrowException_GivenActionIsNull()
        {
            Func<Task> act = async () => await _sut.Process(_aggregate, null);

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

            await _sut.Process(_aggregate, async order => await order.CreditAccount(_creditAmount));

            _eventStoreMock.Verify(x => x.SaveEvents(_aggregateId, It.Is<IEnumerable<object>>(y => VerifyUncommittedEvents(y, expectedEvents))), Times.Once);
        }

        [Test]
        public async Task NotSaveUncommittedEvents_GivenAggregateDoesNotHaveUncommittedEvents()
        {
            await _sut.Process(_aggregate, order => order.DoNothing());

            _eventStoreMock.Verify(x => x.SaveEvents(It.IsAny<string>(), It.IsAny<IEnumerable<object>>()), Times.Never);
        }

        private static bool VerifyUncommittedEvents(IEnumerable<object> events, IEnumerable<object> expectedEvents)
        {
            events.Should().BeEquivalentTo(expectedEvents);
            return true;
        }
    }
}
