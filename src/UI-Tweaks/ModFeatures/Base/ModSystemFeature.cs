using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace BitzArt.UI.Tweaks;

public abstract class ModSystemFeature<TModSystem, TConfig> : ModSystemFeature<TModSystem>
    where TModSystem : ModSystem
{
    protected TConfig Config { get; private init; }

    public ModSystemFeature(TModSystem modSystem, TConfig config) : base(modSystem)
    {
        Config = config;
    }
}

public abstract class ModSystemFeature<TModSystem> : ModSystemFeature
    where TModSystem : ModSystem
{
    protected TModSystem ModSystem { get; private init; }

    public ModSystemFeature(TModSystem modSystem)
    {
        ModSystem = modSystem;
    }
}

public abstract class ModSystemFeature : IDisposable
{
    public virtual string Name => GetType().Name;

    public abstract bool ShouldLoad(EnumAppSide forSide);

    public virtual void Start(ICoreClientAPI clientApi)
        => throw new NotSupportedException($"The mod system feature '{Name}' does not support client-side execution.");

    public virtual void Start(ICoreServerAPI serverApi)
        => throw new NotSupportedException($"The mod system feature '{Name}' does not support server-side execution.");

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
