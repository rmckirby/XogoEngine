using System;
using System.Linq;
using System.IO;

namespace XogoEngine.Graphics
{
    public sealed class SpriteSheet : IDisposable
    {
        private TextureAtlas textureAtlas;
        private bool isDisposed = false;

        public SpriteSheet(ITexture texture, string dataFilePath)
            : this(texture, dataFilePath, new TexturePackerParser())
        {
        }

        internal SpriteSheet(ITexture texture, string dataFilePath, TexturePackerParser parser)
        {
            ValidateArguments(texture, dataFilePath, parser);
            this.Texture = texture;
            this.textureAtlas = parser.Parse(dataFilePath);
        }

        public ITexture Texture { get; }
        public TextureRegion[] TextureRegions
        {
            get { return textureAtlas.TextureRegions.ToArray(); }
        }
        public bool IsDisposed { get { return isDisposed; } }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            Texture.Dispose();
            isDisposed = true;
            GC.SuppressFinalize(this);
        }

        private void ValidateArguments(ITexture texture, string dataFilePath, TexturePackerParser parser)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }
            if (texture.IsDisposed)
            {
                throw new ObjectDisposedException(texture.GetType().FullName);
            }
            if (!File.Exists(dataFilePath))
            {
                throw new FileNotFoundException(nameof(dataFilePath));
            }
        }
    }
}
