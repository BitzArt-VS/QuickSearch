using BitzArt.UI.Tweaks.Gui;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Tests;

public class GuiComponentMeasurementTests
{
    [Fact]
    public void DefaultMeasureCollapsesWhenComponentHasNoChildren()
    {
        var component = new TestContainer();
        Mount(component);

        var measured = component.Measure(100, 100);

        Assert.Equal(new GuiMeasuredSize(0, 0), measured);
    }

    [Fact]
    public void DefaultMeasureStacksRelativeChildrenVertically()
    {
        var root = new TestContainer();
        var first = new FixedMeasureComponent(10, 5);
        var second = new FixedMeasureComponent(7, 11);
        first.LayoutParameters.Margin = new GuiThickness(1, 2, 3, 4);
        second.LayoutParameters.Padding = new GuiThickness(vertical: 1, horizontal: 2);

        Mount(root,
            Slot(first),
            Slot(second));

        var measured = root.Measure(100, 100);

        Assert.Equal(new GuiMeasuredSize(16, 22), measured);
    }

    [Fact]
    public void DefaultMeasureStacksRelativeChildrenHorizontally()
    {
        var root = new TestContainer();
        root.LayoutParameters.Direction = GuiDirection.Horizontal;

        Mount(root,
            Slot(new FixedMeasureComponent(10, 5)),
            Slot(new FixedMeasureComponent(7, 11)));

        var measured = root.Measure(100, 100);

        Assert.Equal(new GuiMeasuredSize(17, 11), measured);
    }

    [Fact]
    public void DefaultMeasureInlinesTransparentNodes()
    {
        var root = new TestContainer();

        Mount(root,
            Slot(new TransparentNode(),
                Slot(new FixedMeasureComponent(10, 5))));

        var measured = root.Measure(100, 100);

        Assert.Equal(new GuiMeasuredSize(10, 5), measured);
    }

    [Fact]
    public void DefaultMeasureSkipsAbsoluteChildren()
    {
        var root = new TestContainer();
        var absolute = new FixedMeasureComponent(10, 5);
        absolute.LayoutParameters.Positioning = GuiComponentPositioning.Absolute;

        Mount(root, Slot(absolute));

        var measured = root.Measure(100, 100);

        Assert.Equal(new GuiMeasuredSize(0, 0), measured);
    }

    [Fact]
    public void BoundedFillChildDoesNotCallMeasure()
    {
        var root = new TestContainer();
        var child = new ThrowingMeasureComponent();
        child.LayoutParameters.WidthMode = GuiSizeMode.Fill;
        child.LayoutParameters.HeightMode = GuiSizeMode.Fill;

        Mount(root, Slot(child));

        var measured = root.Measure(100, 50);

        Assert.Equal(new GuiMeasuredSize(100, 50), measured);
    }

    [Fact]
    public void UnboundedFillChildFallsBackToContentMeasurement()
    {
        var root = new TestContainer();
        var child = new TestContainer();
        child.LayoutParameters.WidthMode = GuiSizeMode.Fill;
        child.LayoutParameters.HeightMode = GuiSizeMode.Fill;

        Mount(root,
            Slot(child,
                Slot(new FixedMeasureComponent(12, 6))));

        var measured = root.Measure(double.PositiveInfinity, double.PositiveInfinity);

        Assert.Equal(new GuiMeasuredSize(12, 6), measured);
    }

    [Fact]
    public void OverrideCanCombineIntrinsicAndDefaultChildMeasurement()
    {
        var component = new IntrinsicAndChildrenComponent(10, 4);
        Mount(component,
            Slot(new FixedMeasureComponent(25, 6)));

        var measured = component.Measure(100, 100);

        Assert.Equal(new GuiMeasuredSize(25, 6), measured);
    }

    [Fact]
    public void PublicMeasureContentMatchesDefaultGuiComponentMeasurement()
    {
        var root = new TestContainer();
        root.LayoutParameters.Direction = GuiDirection.Horizontal;

        var childA = new FixedMeasureComponent(10, 5);
        childA.LayoutParameters.Margin = new GuiThickness(1, 2, 3, 4);

        var childB = new FixedMeasureComponent(7, 11);
        childB.LayoutParameters.Padding = new GuiThickness(vertical: 1, horizontal: 2);

        var transparent = new TransparentNode();

        Mount(root,
            Slot(childA),
            Slot(transparent,
                Slot(childB)));

        var measured = GuiComponentLayout.MeasureContent(
            root.RenderSlot.Children,
            100,
            100,
            root.LayoutParameters.Direction);

        Assert.Equal(root.Measure(100, 100), measured);
    }

    [Fact]
    public void DirectImplementorCanReusePublicMeasurementHelpers()
    {
        var root = new ExternalBaseComponent();

        Mount(root,
            Slot(new FixedMeasureComponent(12, 6)),
            Slot(new FixedMeasureComponent(8, 4)));

        var measured = root.Measure(100, 100);

        Assert.Equal(new GuiMeasuredSize(12, 10), measured);
    }

    private static TestSlot Slot(IGuiNode node, params TestSlot[] children)
        => new(node, children);

    private static void Mount(IGuiNode node, params TestSlot[] children)
    {
        var rootSlot = Slot(node, children);
        rootSlot.AttachRecursive();
    }

    private sealed class TestContainer : GuiComponent
    {
        public IGuiComponentSlot RenderSlot => GetAttachedRenderHandle(nameof(RenderSlot)).Slot;
    }

    private sealed class TransparentNode : GuiNode;

    private sealed class ExternalBaseComponent : IGuiComponent
    {
        private IGuiRenderHandle? _renderHandle;

        public GuiComponentLayoutParameters LayoutParameters { get; } = new();
        public GuiRenderFragment RenderFragment { get; } = _ => { };
        public IGuiComponentSlot RenderSlot => _renderHandle!.Slot;

        public void Attach(IGuiRenderHandle renderHandle, ICoreClientAPI clientApi)
            => _renderHandle = renderHandle;

        public GuiMeasuredSize Measure(double availableWidth, double availableHeight)
            => GuiComponentLayout.MeasureContent(
                _renderHandle!.Slot.Children,
                availableWidth,
                availableHeight,
                LayoutParameters.Direction);
    }

    private sealed class FixedMeasureComponent(double width, double height) : GuiComponent
    {
        public override GuiMeasuredSize Measure(double availableWidth, double availableHeight)
            => new(width, height);
    }

    private sealed class ThrowingMeasureComponent : GuiComponent
    {
        public override GuiMeasuredSize Measure(double availableWidth, double availableHeight)
            => throw new InvalidOperationException("Measure should not be called for bounded fill sizing.");
    }

    private sealed class IntrinsicAndChildrenComponent(double width, double height) : GuiComponent
    {
        public override GuiMeasuredSize Measure(double availableWidth, double availableHeight)
        {
            var children = base.Measure(availableWidth, availableHeight);
            return new GuiMeasuredSize(
                Math.Max(width, children.Width),
                Math.Max(height, children.Height));
        }
    }

    private sealed class TestSlot(IGuiNode node, IReadOnlyList<TestSlot> children) : IGuiComponentSlot
    {
        private readonly IReadOnlyList<IGuiComponentSlot> _children = children;

        public IGuiNode Node { get; } = node;
        public IReadOnlyList<IGuiComponentSlot> Children => _children;

        public void AttachRecursive()
        {
            Node.Attach(new TestRenderHandle(this), clientApi: null!);

            for (int i = 0; i < children.Count; i++)
            {
                children[i].AttachRecursive();
            }
        }
    }

    private sealed class TestRenderHandle(IGuiComponentSlot slot) : IGuiRenderHandle
    {
        public ICoreClientAPI ClientApi => null!;
        public IGuiComponentSlot Slot { get; } = slot;

        public void RequestReconcile(GuiRenderFragment renderFragment) { }
        public void RequestArrange() { }
        public void RequestPaint() { }
        public void RequestRender() { }

        public bool TryGetCascadingValue<T>(out T value)
            => TryGetCascadingValue(name: null, out value);

        public bool TryGetCascadingValue<T>(string? name, out T value)
        {
            value = default!;
            return false;
        }
    }
}
