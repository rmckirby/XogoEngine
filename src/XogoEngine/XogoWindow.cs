using OpenTK.Platform;
using OpenTK.Graphics.OpenGL4;
using System;
using XogoEngine.OpenGL.Adapters;

namespace XogoEngine
{
    public class XogoWindow : IDisposable
    {
        private readonly IGameWindow gameWindow;
        private readonly IGladapter adapter;

        private bool isDisposed = false;

        public XogoWindow(int width, int height, string title)
        {
            /* need to chain to internal constructor here, with concrete instances
             * once they are in place */
            Width = width;
            Height = height;
            Title = title;
        }

        internal XogoWindow(IGameWindow gameWindow, IGladapter adapter)
        {
            if (gameWindow == null)
            {
                throw new ArgumentNullException(nameof(gameWindow));
            }
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }
            this.gameWindow = gameWindow;
            this.adapter = adapter;
            AddEventHandles();
        }

        public int Width
        {
            get { return gameWindow.Width; }
            set { gameWindow.Width = value; }
        }
        public int Height
        {
            get { return gameWindow.Height; }
            set { gameWindow.Height = value; }
        }
        public string Title
        {
            get { return gameWindow.Title; }
            set { gameWindow.Title = value; }
        }
        public bool IsDisposed { get { return isDisposed; } }

        protected virtual void Load() { }
        protected virtual void Update(double delta) { }
        protected virtual void Render(double delta) { }
        protected virtual void Unload() { }

        public void Run()
        {
            ThrowIfDisposed();
            gameWindow.Run();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            Dispose(true);
            isDisposed = true;
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                gameWindow.Dispose();
            }
        }

        private void AddEventHandles()
        {
            gameWindow.Load += (sender, e) => Load();
            gameWindow.Unload += (sender, e) => Unload();
            gameWindow.UpdateFrame += (sender, e) => Update(e.Time);
            gameWindow.RenderFrame += (sender, e) => {
                adapter.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                Render(e.Time);
                gameWindow.SwapBuffers();
            };
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
