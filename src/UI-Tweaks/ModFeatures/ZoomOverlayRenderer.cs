using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks;

internal sealed class ZoomOverlayRenderer : IRenderer, IDisposable
{
    private const string ShaderProgramName = "uitweakszoomoverlay";

    private readonly ICoreClientAPI _clientApi;
    private readonly MeshRef _quadMesh;

    private IShaderProgram? _shaderProgram;
    private bool _isRegistered;
    private float _zoomProgress;
    private float _vignetteStrength;

    public double RenderOrder => 1.1;

    public int RenderRange => 1;

    public ZoomOverlayRenderer(ICoreClientAPI clientApi)
    {
        _clientApi = clientApi;

        var quadMeshData = QuadMeshUtil.GetCustomQuadModelData(-1, -1, 0, 2, 2);
        quadMeshData.Rgba = null;
        _quadMesh = clientApi.Render.UploadMesh(quadMeshData);

        LoadShader();
        clientApi.Event.ReloadShader += LoadShader;
    }

    public void SetZoom(float zoomProgress, float vignetteStrength)
    {
        _zoomProgress = Math.Clamp(zoomProgress, 0, 1);
        _vignetteStrength = Math.Max(0, vignetteStrength);

        if (_zoomProgress > 0 && _vignetteStrength > 0)
        {
            RegisterRenderer();
            return;
        }

        UnregisterRenderer();
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (_shaderProgram is null || _zoomProgress <= 0 || _vignetteStrength <= 0)
        {
            return;
        }

        var previousShaderProgram = _clientApi.Render.CurrentActiveShader;
        previousShaderProgram.Stop();

        _shaderProgram.Use();
        _clientApi.Render.GlToggleBlend(true, EnumBlendMode.Standard);
        _shaderProgram.Uniform("zoomProgress", _zoomProgress);
        _shaderProgram.Uniform("vignetteStrength", _vignetteStrength);
        _clientApi.Render.RenderMesh(_quadMesh);
        _shaderProgram.Stop();

        previousShaderProgram.Use();
    }

    public void Dispose()
    {
        UnregisterRenderer();

        _clientApi.Event.ReloadShader -= LoadShader;

        _shaderProgram?.Dispose();
        _shaderProgram = null;

        _clientApi.Render.DeleteMesh(_quadMesh);

        GC.SuppressFinalize(this);
    }

    private void RegisterRenderer()
    {
        if (_isRegistered)
        {
            return;
        }

        _clientApi.Event.RegisterRenderer(this, EnumRenderStage.Ortho);
        _isRegistered = true;
    }

    private void UnregisterRenderer()
    {
        if (!_isRegistered)
        {
            return;
        }

        _clientApi.Event.UnregisterRenderer(this, EnumRenderStage.Ortho);
        _isRegistered = false;
    }

    private bool LoadShader()
    {
        _shaderProgram?.Dispose();

        var shaderProgram = _clientApi.Shader.NewShaderProgram();
        shaderProgram.VertexShader = _clientApi.Shader.NewShader(EnumShaderType.VertexShader);
        shaderProgram.FragmentShader = _clientApi.Shader.NewShader(EnumShaderType.FragmentShader);
        shaderProgram.VertexShader.Code = GetVertexShaderCode();
        shaderProgram.FragmentShader.Code = GetFragmentShaderCode();

        _clientApi.Shader.RegisterMemoryShaderProgram(ShaderProgramName, shaderProgram);

        _shaderProgram = shaderProgram;

        return shaderProgram.Compile();
    }

    private static string GetVertexShaderCode()
    {
        return """
            #version 330 core
            #extension GL_ARB_explicit_attrib_location: enable

            layout(location = 0) in vec3 vertex;

            out vec2 uv;

            void main(void) {
                gl_Position = vec4(vertex.xy, 0, 1);
                uv = (vertex.xy + 1.0) / 2.0;
            }
            """;
    }

    private static string GetFragmentShaderCode()
    {
        return """
            #version 330 core

            in vec2 uv;
            out vec4 outColor;

            uniform float zoomProgress;
            uniform float vignetteStrength;

            void main(void) {
                float strength = max(vignetteStrength, 0.0);
                float rangeStrength = clamp(strength, 0.0, 1.0);
                float extraRangeStrength = clamp(strength - 1.0, 0.0, 1.0);
                float distanceFromCenter = distance(uv.xy, vec2(0.5, 0.5));
                float vignetteStart = mix(0.34, 0.12, rangeStrength) - extraRangeStrength * 0.05;
                float vignetteEnd = mix(0.78, 0.58, rangeStrength) - extraRangeStrength * 0.10;
                float edgeDarkness = smoothstep(vignetteStart, vignetteEnd, distanceFromCenter);
                float alphaStrength = min(strength, 1.0);
                float alpha = edgeDarkness * alphaStrength * clamp(zoomProgress, 0.0, 1.0);
                outColor = vec4(0.0, 0.0, 0.0, alpha);
            }
            """;
    }
}
