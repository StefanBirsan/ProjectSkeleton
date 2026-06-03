using Silk.NET.Maths;
using Silk.NET.SDL;

namespace TheAdventure;

public sealed class GameRenderer : IDisposable
{
    private readonly Sdl _sdl;
    private readonly IntPtr _rendererHandle;
    private bool _disposed;

    public int WindowWidth { get; }
    public int WindowHeight { get; }

    public GameRenderer(Sdl sdl, IntPtr rendererHandle, int windowWidth, int windowHeight)
    {
        _sdl = sdl;
        _rendererHandle = rendererHandle;
        WindowWidth = windowWidth;
        WindowHeight = windowHeight;
    }

    public void Clear(byte r = 0, byte g = 0, byte b = 0)
    {
        unsafe
        {
            _sdl.SetRenderDrawColor((Renderer*)_rendererHandle, r, g, b, 255);
            _sdl.RenderClear((Renderer*)_rendererHandle);
        }
    }

    public void FillRect(int x, int y, int w, int h, byte r, byte g, byte b)
    {
        unsafe
        {
            var renderer = (Renderer*)_rendererHandle;
            _sdl.SetRenderDrawColor(renderer, r, g, b, 255);
            var rect = new Rectangle<int>(x, y, w, h);
            _sdl.RenderFillRect(renderer, ref rect);
        }
    }

    public void DrawRect(int x, int y, int w, int h, byte r, byte g, byte b)
    {
        unsafe
        {
            var renderer = (Renderer*)_rendererHandle;
            _sdl.SetRenderDrawColor(renderer, r, g, b, 255);
            var rect = new Rectangle<int>(x, y, w, h);
            _sdl.RenderDrawRect(renderer, ref rect);
        }
    }

    public void DrawLine(int x1, int y1, int x2, int y2, byte r, byte g, byte b)
    {
        unsafe
        {
            _sdl.SetRenderDrawColor((Renderer*)_rendererHandle, r, g, b, 255);
            _sdl.RenderDrawLine((Renderer*)_rendererHandle, x1, y1, x2, y2);
        }
    }

    public void Present()
    {
        unsafe { _sdl.RenderPresent((Renderer*)_rendererHandle); }
    }

    public void DrawText(string text, int x, int y, byte r, byte g, byte b, int scale = 2)
    {
        int cx = x;
        foreach (char ch in text.ToUpperInvariant())
        {
            BitmapFont.DrawChar(this, ch, cx, y, r, g, b, scale);
            cx += (BitmapFont.CharWidth + 1) * scale;
        }
    }

    public int TextWidth(string text, int scale = 2) =>
        text.Length * (BitmapFont.CharWidth + 1) * scale;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
