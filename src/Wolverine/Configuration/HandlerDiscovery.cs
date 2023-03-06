using System.Reflection;
using JasperFx.CodeGeneration;
using JasperFx.Core;
using JasperFx.Core.Reflection;
using JasperFx.TypeDiscovery;
using Wolverine.Attributes;
using Wolverine.Persistence.Sagas;
using Wolverine.Runtime.Handlers;

namespace Wolverine.Configuration;

public sealed partial class HandlerDiscovery
{
    private readonly IList<Type> _explicitTypes = new List<Type>();

    private readonly CompositeFilter<MethodInfo> _methodIncludes = new();
    private readonly CompositeFilter<MethodInfo> _methodExcludes = new();

    private readonly string[] _validMethods =
    {
        HandlerChain.Handle, HandlerChain.Handles, HandlerChain.Consume, HandlerChain.Consumes, SagaChain.Orchestrate,
        SagaChain.Orchestrates, SagaChain.Start, SagaChain.Starts, SagaChain.StartOrHandle, SagaChain.StartsOrHandles,
        SagaChain.NotFound
    };

    private bool _conventionalDiscoveryDisabled;

    private readonly TypeQuery _handlerQuery = new(TypeClassification.Concretes | TypeClassification.Closed);
    private readonly TypeQuery _messageQuery = new(TypeClassification.Concretes | TypeClassification.Closed);

    public HandlerDiscovery()
    {
        specifyHandlerMethodRules();

        specifyHandlerDiscovery();

        _messageQuery.Excludes.IsStatic();
        _messageQuery.Includes.Implements<IMessage>();
        _messageQuery.Includes.WithAttribute<WolverineMessageAttribute>();
        _messageQuery.Excludes.IsNotPublic();
    }

    private void specifyHandlerMethodRules()
    {
        foreach (var methodName in _validMethods)
        {
            _methodIncludes.WithCondition($"Method name is '{methodName}' (case sensitive)", m => m.Name == methodName);

            var asyncName = methodName + "Async";
            _methodIncludes.WithCondition($"Method name is '{asyncName}' (case sensitive)", m => m.Name == asyncName);
        }

        _methodIncludes.WithCondition("Has attribute [WolverineHandler]", m => m.HasAttribute<WolverineHandlerAttribute>());

        _methodExcludes.WithCondition("Method is declared by object", method => method.DeclaringType == typeof(object));
        _methodExcludes.WithCondition("IDisposable.Dispose()", method => method.Name == nameof(IDisposable.Dispose));
        _methodExcludes.WithCondition("IAsyncDisposable.DisposeAsync()",
            method => method.Name == nameof(IAsyncDisposable.DisposeAsync));
        _methodExcludes.WithCondition("Contains Generic Parameters", method => method.ContainsGenericParameters);
        _methodExcludes.WithCondition("Special Name", method => method.IsSpecialName);
        _methodExcludes.WithCondition("Has attribute [WolverineIgnore]",
            method => method.HasAttribute<WolverineIgnoreAttribute>());
        
        
        
        _methodExcludes.WithCondition("Has no arguments", m => !m.GetParameters().Any());
        
        _methodExcludes.WithCondition("Cannot determine a valid message type",m => m.MessageType() == null);
        
        _methodExcludes.WithCondition("Returns a primitive type", m => m.ReturnType != typeof(void) && m.ReturnType.IsPrimitive);
    }

    private void specifyHandlerDiscovery()
    {
        _handlerQuery.Includes.WithNameSuffix(HandlerChain.HandlerSuffix);
        _handlerQuery.Includes.WithNameSuffix(HandlerChain.ConsumerSuffix);
        _handlerQuery.Includes.Implements<Saga>();
        _handlerQuery.Includes.Implements<IWolverineHandler>();
        _handlerQuery.Includes.WithAttribute<WolverineHandlerAttribute>();

        _handlerQuery.Excludes.WithCondition("Is not a public type", t => isNotPublicType(t));
        _handlerQuery.Excludes.WithAttribute<WolverineIgnoreAttribute>();
    }

    private static bool isNotPublicType(Type type)
    {
        if (type.IsPublic) return false;
        if (type.IsNestedPublic) return false;

        return true;
    }


    internal IList<Assembly> Assemblies { get; } = new List<Assembly>();

    /// <summary>
    /// Customize the conventional filtering on the handler type discovery 
    /// </summary>
    /// <param name="configure"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public HandlerDiscovery CustomizeHandlerDiscovery(Action<TypeQuery> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(_handlerQuery);
        return this;
    }
    
    /// <summary>
    /// Disables *all* conventional discovery of message handlers from type scanning. This is mostly useful for
    /// testing scenarios or folks who just really want to have full control over everything!
    /// </summary>
    public HandlerDiscovery DisableConventionalDiscovery(bool value = true)
    {
        _conventionalDiscoveryDisabled = value;
        return this;
    }

    internal IReadOnlyList<Type> FindAllMessages(HandlerGraph handlers)
    {
        return findAllMessages(handlers).Distinct().ToList();
    }

    internal IEnumerable<Type> findAllMessages(HandlerGraph handlers)
    {
        foreach (var chain in handlers.Chains)
        {
            yield return chain.MessageType;

            foreach (var publishedType in chain.PublishedTypes())
            {
                yield return publishedType;
            }
        }

        var discovered = _messageQuery.Find(Assemblies);
        foreach (var type in discovered)
        {
            yield return type;
        }
    }

    internal (Type, MethodInfo)[] FindCalls(WolverineOptions options)
    {
        if (_conventionalDiscoveryDisabled)
        {
            return _explicitTypes.SelectMany(actionsFromType).ToArray();
        }

        if (options.ApplicationAssembly != null)
        {
            Assemblies.Fill(options.ApplicationAssembly);
        }
        
        return _handlerQuery.Find(Assemblies)
            .Concat(_explicitTypes)
            .Distinct()
            .SelectMany(actionsFromType).ToArray();
    }

    private IEnumerable<(Type, MethodInfo)> actionsFromType(Type type)
    {
        return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.DeclaringType != typeof(object)).ToArray()
            .Where(m => _methodIncludes.Matches(m) && !_methodExcludes.Matches(m))
            .Select(m => (type, m));
    }

    /// <summary>
    ///     Find Handlers from concrete classes from the given
    ///     assembly
    /// </summary>
    /// <param name="assembly"></param>
    public HandlerDiscovery IncludeAssembly(Assembly assembly)
    {
        Assemblies.Add(assembly);
        return this;
    }

    /// <summary>
    ///     Include a single type "T"
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public HandlerDiscovery IncludeType<T>()
    {
        return IncludeType(typeof(T));
    }

    /// <summary>
    ///     Include a single handler type
    /// </summary>
    /// <param name="type"></param>
    public HandlerDiscovery IncludeType(Type type)
    {
        if (type.IsNotPublic)
        {
            throw new ArgumentOutOfRangeException(nameof(type),
                "Handler types must be public, concrete, and closed (not generic) types");
        }
        
        if (!type.IsStatic() && (type.IsNotConcrete() || type.IsOpenGeneric()))
        {
            throw new ArgumentOutOfRangeException(nameof(type),
                "Handler types must be public, concrete, and closed (not generic) types");
        }

        _explicitTypes.Fill(type);

        return this;
    }

    /// <summary>
    /// Customize how messages are discovered through type scanning. Note that any message
    /// type that is handled by this application or returned as a cascading message type
    /// will be discovered automatically
    /// </summary>
    /// <param name="customize"></param>
    /// <returns></returns>
    public HandlerDiscovery CustomizeMessageDiscovery(Action<TypeQuery> customize)
    {
        if (customize == null)
        {
            throw new ArgumentNullException(nameof(customize));
        }

        customize(_messageQuery);

        return this;
    }
}
