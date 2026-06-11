using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using ArtFrame.ArtTypes;
using Microsoft.Xna.Framework.Graphics;

using Color = ArtFrame.ArtTypes.Color;
using Vector2 = ArtFrame.ArtTypes.Vector2;
using Rectangle = ArtFrame.ArtTypes.Rectangle;

namespace ArtFrame.UserInterface
{
    public enum VideoPlaybackState { Stopped, Playing, Paused }
    public class VideoFrame : ArtObject, IDisposable
    {
        public float rotation { get; set; } = 0f;
        public ObjectFit fit { get; set; } = ObjectFit.None;
        public float alpha { get; set; } = 1f;
        public Color color { get; set; } = Color.White;
        public float PlaybackRate
        {
            get => _media == null ? 1f : _mediaPlayer != null ? (float)_mediaPlayer.Rate : 0;
            set { if (_mediaPlayer != null) _mediaPlayer.SetRate(value); }
        }

        public List<ArtObject> children { get; set; } = new();
        public List<IFrameModifier> modifiers { get; set; } = new();
        public Action<VideoFrame, float>? onUpdate { get; set; }

        // LibVLC
        private static readonly LibVLC _libVLC = new("--avcodec-hw=none", "--quiet");
        private MediaPlayer? _mediaPlayer;
        private LibVLCSharp.Shared.Media? _media;
        private string? _videoPath;
        private bool _disposed;

        // Frame buffer
        private IntPtr _frameBufferPtr = IntPtr.Zero;
        private byte[]? _pendingFrame;
        private bool _newFrameReady;
        private int _videoWidth;
        private int _videoHeight;
        private Microsoft.Xna.Framework.Graphics.Texture2D? _texture;
        private readonly object _frameLock = new();

        public string? VideoPath => _videoPath;
        public VideoPlaybackState PlaybackState => _mediaPlayer?.State switch
        {
            VLCState.Playing => VideoPlaybackState.Playing,
            VLCState.Paused  => VideoPlaybackState.Paused,
            _                => VideoPlaybackState.Stopped
        };

        public bool IsLooped { get; set; } = true;
        public long PositionMs
        {
            get => _mediaPlayer == null ? 0L : _mediaPlayer.Time;
            set { if (_mediaPlayer != null) _mediaPlayer.Time = value; }
        }
        public float Volume
        {
            get => _mediaPlayer == null ? 1f : _mediaPlayer.Volume / 100f;
            set { if (_mediaPlayer != null) _mediaPlayer.Volume = (int)(value * 100f); }
        }

        public VideoFrame() { }

        public void Play(string path)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(VideoFrame));

            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[VideoFrame] File not found: {fullPath}");
                return;
            }

            // Only reload if path changed
            if (_videoPath != fullPath)
            {
                Stop();
                DisposePlayer();

                _videoPath = fullPath;
                _media = new LibVLCSharp.Shared.Media(_libVLC, new Uri(fullPath), ":avcodec-hw=none");
                _mediaPlayer = new MediaPlayer(_media);

                // Parse video dimensions before playing
                _media.Parse(MediaParseOptions.ParseLocal).Wait();
                var track = _media.Tracks[0]; // first video track
                _videoWidth  = (int)track.Data.Video.Width;
                _videoHeight = (int)track.Data.Video.Height;

                int bufSize = _videoWidth * _videoHeight * 4; // RGBA
                if (_frameBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_frameBufferPtr);
                }
                _frameBufferPtr = Marshal.AllocHGlobal(bufSize);
                _pendingFrame = new byte[bufSize];

                // Hook raw frame callback
                _mediaPlayer.SetVideoCallbacks(LockCallback, UnlockCallback, DisplayCallback);
                _mediaPlayer.SetVideoFormat("RGBA", (uint)_videoWidth, (uint)_videoHeight, (uint)(_videoWidth * 4));

                if (IsLooped)
                    _mediaPlayer.EndReached += (_, _) =>
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                        {
                            _mediaPlayer.Stop();
                            _mediaPlayer.Play();
                        });
                    };
            }

            if (_mediaPlayer?.State == VLCState.Paused)
                _mediaPlayer.SetPause(false);
            else
                _mediaPlayer?.Play();
        }

        // LibVLC lock: give it our buffer pointer
        private IntPtr LockCallback(IntPtr opaque, IntPtr planes)
        {
            if (_frameBufferPtr != IntPtr.Zero)
            {
                Marshal.WriteIntPtr(planes, _frameBufferPtr);
            }
            return IntPtr.Zero;
        }

        // LibVLC unlock: called when VLC is done writing to the buffer
        private void UnlockCallback(IntPtr opaque, IntPtr picture, IntPtr planes)
        {
            // Nothing to do for unmanaged buffers, but LibVLC needs this callback
            // to mark the picture buffer as unlocked and recycle it back to the picture pool.
        }

        // LibVLC display: frame is ready, copy to pending buffer
        private void DisplayCallback(IntPtr opaque, IntPtr picture)
        {
            lock (_frameLock)
            {
                if (_frameBufferPtr != IntPtr.Zero && _pendingFrame != null)
                {
                    Marshal.Copy(_frameBufferPtr, _pendingFrame, 0, _pendingFrame.Length);
                    _newFrameReady = true;
                }
            }
        }

        public void Pause()
        {
            if (_disposed) return;
            if (_mediaPlayer?.State == VLCState.Playing)
                _mediaPlayer.SetPause(true);
        }

        public void Resume()
        {
            if (_disposed) return;
            if (_mediaPlayer?.State == VLCState.Paused)
                _mediaPlayer.SetPause(false);
        }

        public void Stop()
        {
            if (_disposed) return;
            _mediaPlayer?.Stop();
        }

        public override void Update(float dt)
        {
            if (_disposed) return;
            onUpdate?.Invoke(this, dt);
            foreach (var child in children) child.Update(dt);
        }

        public override void Draw(float dt, Vector2 parentSize, Vector2 parentOrigin)
        {
            if (_disposed || skipDraw) return;

            // Upload new frame to GPU if available
            if (_newFrameReady && _pendingFrame != null && _videoWidth > 0 && _videoHeight > 0)
            {
                lock (_frameLock)
                {
                    if (_texture == null || _texture.Width != _videoWidth || _texture.Height != _videoHeight)
                    {
                        _texture?.Dispose();
                        _texture = new Microsoft.Xna.Framework.Graphics.Texture2D(Art.Instance.GraphicsDevice, _videoWidth, _videoHeight, false, SurfaceFormat.Color);
                    }
                    _texture.SetData(_pendingFrame);
                    _newFrameReady = false;
                }
            }

            Vector2 resolvedSize   = size.Resolve(parentSize);
            Vector2 resolvedPos    = position.Resolve(parentSize);
            Vector2 anchorOffset   = GraphicsHelper.GetAnchorOffset(anchorX, anchorY, resolvedSize);
            Vector2 screenTopLeft  = parentOrigin + resolvedPos - anchorOffset;
            Vector2 objectCenter   = screenTopLeft + resolvedSize / 2f;

            var tint    = new Color(color.R, color.G, color.B, (byte)(alpha * 255f));
            float rads  = Microsoft.Xna.Framework.MathHelper.ToRadians(rotation);

            if (_texture != null)
            {
                if (fit == ObjectFit.Cover)
                {
                    Rectangle srcRect = ComputeCoverSrc(_texture, new Rectangle(0, 0, (int)resolvedSize.X, (int)resolvedSize.Y));
                    Vector2 pivot = new Vector2(srcRect.Width / 2f, srcRect.Height / 2f);
                    Vector2 scale = new Vector2(resolvedSize.X / srcRect.Width, resolvedSize.Y / srcRect.Height);
                    Art.Instance.spriteBatch.Draw(_texture, objectCenter, (Microsoft.Xna.Framework.Rectangle)srcRect, tint, rads, pivot, scale, SpriteEffects.None, 0f);
                }
                else
                {
                    Rectangle destRect = ComputeDestRect(_texture, screenTopLeft, resolvedSize);
                    Vector2 pivot = new Vector2(_texture.Width / 2f, _texture.Height / 2f);
                    Vector2 scale = new Vector2((float)destRect.Width / _texture.Width, (float)destRect.Height / _texture.Height);
                    Art.Instance.spriteBatch.Draw(_texture, objectCenter, null, tint, rads, pivot, scale, SpriteEffects.None, 0f);
                }
            }

            foreach (var mod in modifiers) mod.Apply(children, resolvedSize);
            foreach (var child in children) child.Draw(dt, resolvedSize, screenTopLeft);
        }

        private Rectangle ComputeCoverSrc(Microsoft.Xna.Framework.Graphics.Texture2D texture, Rectangle targetRect)
        {
            float targetAspect = (float)targetRect.Width / targetRect.Height;
            float imageAspect = (float)texture.Width / texture.Height;

            float srcX = 0f, srcY = 0f;
            float srcW = texture.Width, srcH = texture.Height;

            if (imageAspect > targetAspect)
            {
                srcW = texture.Height * targetAspect;
                srcX = (texture.Width - srcW) / 2f;
            }
            else
            {
                srcH = texture.Width / targetAspect;
                srcY = (texture.Height - srcH) / 2f;
            }

            return new Rectangle((int)srcX, (int)srcY, (int)srcW, (int)srcH);
        }

        private Rectangle ComputeDestRect(Microsoft.Xna.Framework.Graphics.Texture2D texture, Vector2 origin, Vector2 resolvedSize)
        {
            switch (fit)
            {
                case ObjectFit.Contain:
                    float scale = Math.Min(resolvedSize.X / texture.Width, resolvedSize.Y / texture.Height);
                    int containW = (int)(texture.Width * scale);
                    int containH = (int)(texture.Height * scale);
                    int containX = (int)(origin.X + (resolvedSize.X - containW) / 2f);
                    int containY = (int)(origin.Y + (resolvedSize.Y - containH) / 2f);
                    return new Rectangle(containX, containY, containW, containH);

                case ObjectFit.None:
                    int noneX = (int)(origin.X + (resolvedSize.X - texture.Width) / 2f);
                    int noneY = (int)(origin.Y + (resolvedSize.Y - texture.Height) / 2f);
                    return new Rectangle(noneX, noneY, texture.Width, texture.Height);

                case ObjectFit.Fill:
                default:
                    return new Rectangle((int)origin.X, (int)origin.Y, (int)resolvedSize.X, (int)resolvedSize.Y);
            }
        }

        private void DisposePlayer()
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _media?.Dispose();
            _mediaPlayer = null;
            _media = null;

            if (_frameBufferPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_frameBufferPtr);
                _frameBufferPtr = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~VideoFrame()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    DisposePlayer();
                    _texture?.Dispose();
                }
                else
                {
                    if (_frameBufferPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(_frameBufferPtr);
                        _frameBufferPtr = IntPtr.Zero;
                    }
                }
                _disposed = true;
            }
        }
    }
}
