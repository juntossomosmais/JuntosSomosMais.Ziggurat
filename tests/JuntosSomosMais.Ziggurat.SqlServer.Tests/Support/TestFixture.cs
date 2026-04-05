using System;
using Xunit;

namespace JuntosSomosMais.Ziggurat.SqlServer.Tests.Support;

[Collection("TestFixture Collection")]
public class TestFixture : IDisposable
{
    private bool _isDisposed;

    public TestFixture()
    {
        Context.Database.EnsureCreated();
        Context.Database.BeginTransaction();
    }

    protected TestDbContext Context { get; } = new();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            Context.Database.RollbackTransaction();
            Context?.Dispose();
        }

        _isDisposed = true;
    }
}
