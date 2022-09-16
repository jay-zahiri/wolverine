using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Baseline.ImTools;
using Wolverine.ErrorHandling;
using Wolverine.Runtime.Routing;
using Lamar;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Wolverine.Logging;
using Wolverine.Persistence.Durability;
using Wolverine.Runtime.Handlers;
using Wolverine.Runtime.Scheduled;

namespace Wolverine.Runtime;

public sealed partial class WolverineRuntime : IWolverineRuntime, IHostedService
{
    private readonly IContainer _container;

    private readonly Lazy<IEnvelopePersistence> _persistence;
    private bool _hasStopped;

    private readonly string _serviceName;
    private readonly int _uniqueNodeId;


    public WolverineRuntime(WolverineOptions options,
        IContainer container,
        ILogger<WolverineRuntime> logger)
    {
        Advanced = options.Advanced;
        Options = options;
        Handlers = options.HandlerGraph;
        Logger = logger;

        _uniqueNodeId = options.Advanced.UniqueNodeId;
        _serviceName = options.ServiceName ?? "WolverineService";

        var provider = container.GetInstance<ObjectPoolProvider>();
        ExecutionPool = provider.Create(this);

        Pipeline = new HandlerPipeline(this, this);

        _persistence = new Lazy<IEnvelopePersistence>(container.GetInstance<IEnvelopePersistence>);

        _container = container;

        Cancellation = Advanced.Cancellation;

        ListenerTracker = new ListenerTracker(logger);
    }

    public ListenerTracker ListenerTracker { get; }

    internal IReadOnlyList<IMissingHandler> MissingHandlers()
    {
        return _container.GetAllInstances<IMissingHandler>();
    }

    public ObjectPool<MessageContext> ExecutionPool { get; }

    public DurabilityAgent? Durability { get; private set; }

    internal HandlerGraph Handlers { get; }

    public CancellationToken Cancellation { get; }

    private ImHashMap<Type, object?> _extensions = ImHashMap<Type, object?>.Empty;

    public T? TryFindExtension<T>() where T : class
    {
        if (_extensions.TryFind(typeof(T), out var raw)) return (T)raw;

        var extension = Options.AppliedExtensions.OfType<T>().FirstOrDefault();
        _extensions = _extensions.AddOrUpdate(typeof(T), extension);

        return extension;
    }

    public AdvancedSettings Advanced { get; }

    public ILogger Logger { get; }

    internal IScheduledJobProcessor ScheduledJobs { get; private set; } = null!;

    public WolverineOptions Options { get; }

    public void ScheduleLocalExecutionInMemory(DateTimeOffset executionTime, Envelope envelope)
    {
        ScheduledJobs.Enqueue(executionTime, envelope);
    }

    public IHandlerPipeline Pipeline { get; }

    public IMessageLogger MessageLogger => this;


    public IEnvelopePersistence Persistence => _persistence.Value;
}
