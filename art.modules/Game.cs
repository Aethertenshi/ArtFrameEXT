using System;
using ArtFrameCore.SdlBindings;
using ArtFrameCore.UserInterface;

namespace ArtFrameCore.Modules
{
    /// <summary>
    /// Base class for the game application. Hides the low-level rendering and event loop,
    /// providing a clean Load/Update lifecycle for developers.
    /// </summary>
    public class Game : UIGroup
    {
        public string Title { get; set; } = "ArtFrame Game";
        public int WindowWidth { get; set; } = 800;
        public int WindowHeight { get; set; } = 600;
        public WindowMode WindowMode { get; set; } = WindowMode.Resizable;

        public Game()
        {
            Name = "GameRoot";
        }

        /// <summary>
        /// Starts the game, initializes the window and renderer, triggers the lifecycle, and runs the main loop.
        /// </summary>
        public void Run()
        {
            if (!Window.Create(Title, WindowWidth, WindowHeight, resizable: WindowMode))
            {
                Console.WriteLine("Failed to initialize game window.");
                return;
            }

            // Trigger the developer's loading/initialization code
            Load();

            // Run the game loop
            while (Window.KeepRunning())
            {
                // Trigger recursive hierarchy updates for all elements and custom actions first!
                base.Update();

                // Trigger game-level custom updates
                OnUpdate();

                // Handle clear, recursive drawing of the hierarchy, and buffer presenting in the backend!
                Renderer.BeginFrame();
                Draw(0, 0, WindowWidth, WindowHeight);
                Renderer.EndFrame();
            }

            // Perform automatic clean shutdown
            Shutdown();
            Window.Close();
        }

        /// <summary>
        /// Overridden by developers to initialize UI, load assets, and construct nested elements.
        /// </summary>
        protected virtual void Load()
        {
        }

        /// <summary>
        /// Overridden by developers to perform frame-by-frame logical calculations.
        /// </summary>
        protected virtual void OnUpdate()
        {
        }

        /// <summary>
        /// Performs application-specific shutdown operations. Override this method to release resources or perform
        /// cleanup tasks when shutting down.
        /// </summary>
        /// <remarks>This method is called during the shutdown process. Derived classes should override
        /// this method to implement any necessary cleanup logic. The base implementation does nothing.</remarks>
        protected virtual void Shutdown()
        {
        }
    }
}
