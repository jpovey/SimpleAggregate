namespace SimpleAggregate.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AutoFixture;
    using Domain;
    using Domain.Events;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class AggregateShould
    {
        private readonly Fixture _fixture = new Fixture();
        private BankAccount _sut;
        private decimal _creditAmount;

        [SetUp]
        public void Setup()
        {
            _creditAmount = _fixture.Create<decimal>();
            _sut = new BankAccount();
        }

        [Test]
        public void AddEventToUncommittedEvents_WhenApplyingEvent()
        {
            _sut.CreditAccount(_creditAmount);

            _sut.UncommittedEvents.Count.Should().Be(1);
            _sut.UncommittedEvents.First().Should().BeEquivalentTo(new AccountCredited { Amount = _creditAmount });
        }

        [Test]
        public void ClearUncommittedEvents()
        {
            _sut.CreditAccount(_creditAmount);

            _sut.ClearUncommittedEvents();

            _sut.UncommittedEvents.Count.Should().Be(0);
        }

        [Test]
        public void ApplyEvent()
        {
            _sut.CreditAccount(_creditAmount);

            _sut.Balance.Should().Be(_creditAmount);
        }

        [Test]
        public void RehydrateAggregate()
        {
            var events = new List<object>
            {
                new AccountCredited { Amount = _creditAmount }
            };

            _sut.Rehydrate(events);

            _sut.Balance.Should().Be(_creditAmount);
        }

        [Test]
        public void NotAddLoadedEventsToUncommittedEvents_WhenHydratingAggregate()
        {
            var events = new List<object>
            {
                new AccountCredited { Amount = _creditAmount }
            };

            _sut.Rehydrate(events);

            _sut.UncommittedEvents.Count.Should().Be(0);
        }

        [Test]
        public void NotThrowException_WhenRehydratingAggregateWithUnregisteredEvent_GivenUnregisteredEventsAreNotForbidden()
        {
            _sut = new BankAccount();

            var events = new List<object>
            {
                new UnregisteredEvent()
            };

            Action act = () => _sut.Rehydrate(events);

            act.Should().NotThrow<UnregisteredEventException>();
        }

        [Test]
        public void ThrowException_WhenRehydratingAggregateWithUnregisteredEvent_GivenUnregisteredEventsAreForbidden()
        {
            const bool forbidUnregisteredEvents = true;
            _sut = new BankAccount(forbidUnregisteredEvents);

            var events = new List<object>
            {
                new UnregisteredEvent()
            };

            Action act = () => _sut.Rehydrate(events);

            act.Should().Throw<UnregisteredEventException>();
        }

        [Test]
        public void NotThrowException_WhenRehydratingAggregateWithRegisteredEvent_GivenUnregisteredEventsAreForbidden()
        {
            const bool forbidUnregisteredEvents = true;
            _sut = new BankAccount(forbidUnregisteredEvents);

            var events = new List<object>
            {
                new AccountCredited { Amount = _creditAmount }
            };

            Action act = () => _sut.Rehydrate(events);

            act.Should().NotThrow<UnregisteredEventException>();
        }

        [Test]
        public void NotThrowException_WhenRehydratingAggregateWithRegisteredEvent_GivenForbidUnregisteredEventsIsSetAsDefault()
        {
            _sut = new BankAccount();

            var events = new List<object>
            {
                new UnregisteredEvent()
            };

            Action act = () => _sut.Rehydrate(events);

            act.Should().NotThrow<UnregisteredEventException>();
        }

        [Test]
        public void ThrowArgumentNullException_WhenTryingToApplyNull()
        {
            Action act = () => _sut.ApplyNullEvent();

            act.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void RehydrateAggregate_GivenMultipleEvents()
        {
            var events = new List<object>
            {
                new AccountCredited { Amount = 100 },
                new AccountDebited { Amount = 25 },
            };

            _sut.Rehydrate(events);

            _sut.Balance.Should().Be(75);
            _sut.UncommittedEvents.Count.Should().Be(0);
        }

        [Test]
        public void PerformCommand_GivenAggregateHasBeenHydrated()
        {
            var events = new List<object>
            {
                new AccountCredited { Amount = 50 },
                new AccountDebited { Amount = 10 },
            };

            _sut.Rehydrate(events);

            _sut.CreditAccount(20);

            _sut.Balance.Should().Be(60);
            _sut.UncommittedEvents.Count.Should().Be(1);
        }

        [Test]
        public void StoreCommittedEventsAgainstTheAggregate()
        {
            var events = new List<object>
            {
                new AccountCredited { Amount = 50 },
                new AccountDebited { Amount = 10 },
            };

            _sut.Rehydrate(events);
            _sut.CommittedEvents.Should().BeEquivalentTo(events);
        }


        [Test]
        public void ApplyEvent_WhenCreatingAggregate()
        {
            var accountId = _fixture.Create<string>();

            _sut = new BankAccount(accountId);

            _sut.UncommittedEvents.Count.Should().Be(1);
            _sut.UncommittedEvents.First().GetType().Should().Be(typeof(AccountCreated));
        }
    }
}
