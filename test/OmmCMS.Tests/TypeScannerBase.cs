using System;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OmmCMS.Engine;
using OmmCMS.Engine.Modularity;
using OmmCMS.Test.Common;

namespace OmmCMS.Tests;

public abstract class TypeScannerBase : TestsBase
{
    protected ITypeScanner typeScanner;

    protected abstract Type[] GetTypes();

    [OneTimeSetUp]
    public override void SetUp()
    {
        base.SetUp();
        typeScanner = new DefaultTypeScanner(
            new[] { typeof(TypeScannerBase).Assembly },
            new ModuleCatalog(Array.Empty<IModuleDescriptor>()),
            NullLogger.Instance);
    }
}