namespace SimpleAggregate.UnitTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Domain;
    using FluentAssertions;
    using Moq;
    using NUnit.Framework;

    public class AggregateProcessorShould
    {
        private readonly Fixture _fixture = new Fixture();
        private string _aggregateId;
        private Mock<IAggregateRepository<BankAccount>> _aggregateRepositoryMock;
        private BankAccount _aggregate;
        private CancellationToken _cancellationToken;
        private AggregateProcessor<BankAccount> _sut;

        [SetUp]
        public void Setup()
        {
            _cancellationToken = new CancellationToken();
            _aggregateId = _fixture.Create<string>();
            _aggregate = new BankAccount();

            _aggregateRepositoryMock = new Mock<IAggregateRepository<BankAccount>>();
            _aggregateRepositoryMock.Setup(x => x.GetAsync(_aggregateId, _cancellationToken)).ReturnsAsync(_aggregate);

            _sut = new AggregateProcessor<BankAccount>(_aggregateRepositoryMock.Object);
        }

        [Test]
        public async Task GetAggregateById()
        {
            await _sut.ProcessAsync(_aggregateId, null, _cancellationToken);

            _aggregateRepositoryMock.Verify(x => x.GetAsync(_aggregateId, CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task ActionCommandAgainstTheLoadedAggregate()
        {
            var creditAmount = _fixture.Create<decimal>();

            await _sut.ProcessAsync(_aggregateId, bankAccount => bankAccount.CreditAccount(creditAmount), _cancellationToken);

            _aggregate.Balance.Should().Be(creditAmount);
        }

        [Test]
        public void NotThrowException_GivenCommandIsNull()
        {
            Func<Task> act = async () => await _sut.ProcessAsync(_aggregateId, null, _cancellationToken);

            act.Should().NotThrow<Exception>();

            _aggregate.Balance.Should().Be(default(decimal));
        }

        [Test]
        public async Task SaveAggregate()
        {
            await _sut.ProcessAsync(_aggregateId, null, _cancellationToken);

            _aggregateRepositoryMock.Verify(x => x.SaveAsync(_aggregate, CancellationToken.None), Times.Once);
        }
    }
}
