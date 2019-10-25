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
    using NUnit.Framework;

    [TestFixture]
    public class AggregateShould
    {
        private readonly Fixture _fixture = new Fixture();
        private string _accountReference;
        private BankAccount _sut;
        private decimal _creditAmount;

        [SetUp]
        public void Setup()
        {
            _accountReference = _fixture.Create<string>();
            _creditAmount = _fixture.Create<decimal>();
            _sut = new BankAccount(_accountReference);
        }

        [Test]
        public void SetAggregateId_WhenCreatingNewAggregate()
        {
            _sut = new BankAccount(_accountReference);

            _sut.AggregateId.Should().Be(_accountReference);
        }

        [Test]
        public void ReturnAggregateId_WhenAccessingBankAccountReference()
        {
            _sut = new BankAccount(_accountReference);

            _sut.AccountReference.Should().Be(_accountReference);
        }

        [Test]
        public void ThrowArgumentNullException_WhenCreatingTheAggregate_GivenTheAggregateIdIsNull()
        {
            Action act = () => _sut = new BankAccount(null);

            act.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task AddEventToUncommittedEvents_WhenApplyingEvent()
        {
            await _sut.CreditAccount(_creditAmount);

            _sut.UncommittedEvents.Count.Should().Be(1);
            _sut.UncommittedEvents.First().Should().BeEquivalentTo(new AccountCredited { Amount = _creditAmount });
        }

        [Test]
        public async Task ClearUncommittedEvents()
        {
            await _sut.CreditAccount(_creditAmount);

            _sut.ClearUncommittedEvents();

            _sut.UncommittedEvents.Count.Should().Be(0);
        }

        [Test]
        public async Task ApplyEvent()
        {
            await _sut.CreditAccount(_creditAmount);

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
        public void NotAddHydratedEventsToUncommittedEvents_WhenHydratingAggregate()
        {
            var events = new List<object>
            {
                new AccountCredited { Amount = _creditAmount }
            };

            _sut.Rehydrate(events);

            _sut.UncommittedEvents.Count.Should().Be(0);
        }

        [Test]
        public void NotThrowException_WhenApplyingUnregisteredEvent_GivenIgnoreUnregisteredEventsIsTrue()
        {
            const bool ignoreUnregisteredEvents = true;
            _sut = new BankAccount(_accountReference, ignoreUnregisteredEvents);

            var events = new List<object>
            {
                new UnregisteredEvent()
            };

            Action act = () => _sut.Rehydrate(events);

            act.Should().NotThrow<UnregisteredEventException>();
        }

        [Test]
        public void ThrowException_WhenApplyingUnregisteredEvent_GivenIgnoreUnregisteredEventsIsFalse()
        {
            const bool ignoreUnregisteredEvents = false;
            _sut = new BankAccount(_accountReference, ignoreUnregisteredEvents);

            var events = new List<object>
            {
                new UnregisteredEvent()
            };

            Action act = () => _sut.Rehydrate(events);

            act.Should().Throw<UnregisteredEventException>();
        }

        [Test]
        public void NotThrowException_WhenApplyingUnregisteredEvent_GivenIgnoreUnregisteredEventsIsSetAsDefault()
        {
            _sut = new BankAccount(_accountReference);

            var events = new List<object>
            {
                new UnregisteredEvent()
            };

            Action act = () => _sut.Rehydrate(events);

            act.Should().NotThrow<UnregisteredEventException>();
        }

        [Test]
        public void ThrowArgumentNullException_WhenApplyingNullEvent()
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
        public async Task PerformCommand_GivenAggregateHasBeenHydrated()
        {
            var events = new List<object>
            {
                new AccountCredited { Amount = 50 },
                new AccountDebited { Amount = 10 },
            };

            _sut.Rehydrate(events);

            await _sut.CreditAccount(20);

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
    }
}
