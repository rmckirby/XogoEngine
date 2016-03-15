using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using OpenTK;
using XogoEngine.Graphics;
using XogoEngine.OpenGL.Vertex;

namespace XogoEngine.Test.Graphics
{
    [TestFixture]
    internal sealed class SpriteBatchTest
    {
        private SpriteBatch spriteBatch;
        private Mock<ISpriteSheet> spriteSheet;
        private Mock<ITexture> texture;

        [SetUp]
        public void SetUp()
        {
            texture = new Mock<ITexture>();
            texture.SetupGet(t => t.Width).Returns(200);
            texture.SetupGet(t => t.Height).Returns(50);
            spriteSheet = new Mock<ISpriteSheet>();
            spriteSheet.SetupGet(s => s.Texture)
                       .Returns(texture.Object);
            spriteSheet.Setup(s => s.GetRegion(It.IsAny<int>()))
                       .Returns(new TextureRegion(2, 2, 15, 20));

            spriteBatch = new SpriteBatch(spriteSheet.Object);
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_OnNullSpriteSheet()
        {
            SpriteSheet nullSheet = null;
            Action construct = () => new SpriteBatch(nullSheet);
            construct.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_CorrectlyInitialises_Instance()
        {
            spriteBatch.ShouldSatisfyAllConditions(
                () => spriteBatch.SpriteSheet.ShouldBe(spriteSheet.Object)
            );
        }

        [Test]
        public void Add_ThrowsArgumentNullException_OnNullSprite()
        {
            Sprite nullSprite = null;
            Action add = () => spriteBatch.Add(nullSprite);
            add.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("sprite");
        }

        [Test]
        public void Add_AddsGivenSprite_ToSpriteList()
        {
            var sprite = new Sprite(spriteSheet.Object.GetRegion(0), 10, 10);
            spriteBatch.Add(sprite);
            spriteBatch.Sprites.ShouldContain(sprite);
        }

        [Test]
        public void Add_ThrowsDuplicateSpriteException_WhenAddingTheSameSpriteAgain()
        {
            var sprite = new Sprite(spriteSheet.Object.GetRegion(0), 10, 10);
            spriteBatch.Add(sprite);
            Action addAgain = () => spriteBatch.Add(sprite);

            addAgain.ShouldThrow<DuplicateSpriteException>().Message.ShouldContain(
                "The given sprite has already been added to this sprite batch"
            );
        }

        [Test]
        public void Add_PreparesSpriteVertices_OnSuccessfulAdd()
        {
            var sprite = new Sprite(spriteSheet.Object.GetRegion(0), 10, 10);
            spriteBatch.Add(sprite);

            /* The sprite batch should take the sprite's texture region
            *  and scale each corner according to the whole texture atlas width/height
            * so that they fall in the range 0-1 (to match the expectations of OpenGL) */

            float scaledWidth = 1.0f / spriteSheet.Object.Texture.Width;
            float scaledHeight = 1.0f / spriteSheet.Object.Texture.Height;
            var region = sprite.TextureRegion;

            // texture packer x,y refer to bottom left position of the texture region
            // whereas, OpenGL coordinate system has (0,0) at the upper-left
            var topLeftCoord = new Vector2(region.X * scaledWidth, (region.Y + region.Height) * scaledHeight);
            var topRightCoord = new Vector2((region.X + region.Width) * scaledWidth, (region.Y + region.Height) * scaledHeight);
            var bottomRightCoord = new Vector2((region.X + region.Width) * scaledWidth, region.Y * scaledHeight);
            var bottomLeftCoord = new Vector2(region.X * scaledWidth, region.Y * scaledHeight);

            // we should also scale the sprite's colour to fall in the range 0-1
            var sColour = sprite.Colour;
            var scaledColour = new Vector4(sColour.R / 255, sColour.G / 255, sColour.B / 255, sColour.A / 255);

            // expected vertex positions
            var topLeftPosition = new Vector2(sprite.X, sprite.Y + scaledHeight);
            var topRightPosition = new Vector2(sprite.X + scaledWidth, sprite.Y + scaledHeight);
            var bottomRightPosition = new Vector2(sprite.X + scaledWidth, sprite.Y);
            var bottomLeftPosition = new Vector2(sprite.X, sprite.Y);

            var topLeftVertex = new VertexPositionColourTexture(topLeftPosition, scaledColour, topLeftCoord);
            var topRightvertex = new VertexPositionColourTexture(topRightPosition, scaledColour, topRightCoord);
            var bottomRightVertex = new VertexPositionColourTexture(bottomRightPosition, scaledColour, bottomRightCoord);
            var bottomLeftVertex = new VertexPositionColourTexture(bottomLeftPosition, scaledColour, bottomLeftCoord);

            sprite.Vertices.ShouldSatisfyAllConditions(
                () => sprite.Vertices[0].ShouldBe(topLeftVertex),
                () => sprite.Vertices[1].ShouldBe(topRightvertex),
                () => sprite.Vertices[2].ShouldBe(bottomRightVertex),
                () => sprite.Vertices[3].ShouldBe(bottomLeftVertex)
            );
        }

        [Test]
        public void Remove_ThrowsArgumentException_WhenSpriteIsNotInBatch()
        {
            var sprite = new Sprite(spriteSheet.Object.GetRegion(0), 10, 10);
            Action remove = () => spriteBatch.Remove(sprite);

            remove.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Remove_RemovesGivenSprite_FromExistingSpriteList()
        {
            var sprite = new Sprite(spriteSheet.Object.GetRegion(0), 10, 10);
            spriteBatch.Add(sprite);

            spriteBatch.Sprites.ShouldContain(sprite);
            spriteBatch.Remove(sprite);
            spriteBatch.Sprites.ShouldNotContain(sprite);
        }

        [Test]
        public void Add_ThrowsBatchSizeExceededException_OnAddingTooManySprites()
        {
            const int batchSize = 100;
            for (int i = 0; i < batchSize; i++)
            {
                var sprite = new Sprite(spriteSheet.Object.GetRegion(0), i, i);
                spriteBatch.Add(sprite);
            }
            var illegalSprite = new Sprite(spriteSheet.Object.GetRegion(0), 101, 101);
            Action add = () => spriteBatch.Add(illegalSprite);

            add.ShouldThrow<SpriteBatchSizeExceededException>();
        }

        [Test]
        public void Spritebatch_IsNotDisposed_AfterConstruction()
        {
            spriteBatch.IsDisposed.ShouldBeFalse();
        }

        [Test]
        public void SpriteBatch_IsDisposed_AfterDisposal()
        {
            spriteBatch.Dispose();
            spriteBatch.IsDisposed.ShouldBeTrue();
        }

        [Test]
        public void SpriteSheet_IsDisposed_OnDisposal()
        {
            spriteBatch.Dispose();
            spriteSheet.Verify(s => s.Dispose());
        }
    }
}