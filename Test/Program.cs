using Somnium.Framework;
using System;

namespace Test
{
    public static class Program
    {
        private static Application application;
        private static float recordTime = 0f;

        [STAThread]
        public static void Main(string[] args)
        {
            using (application = Application.New("Test", new Point(1920, 1080), "Window", Backends.Vulkan))
            {
                application.Update = Update;
                application.Draw = Render;
                application.Start();
            }
        }
        private static void OnLoad()
        {
        }

        private static void Render(float deltaTime)
        {
            //Here all rendering should be done.
        }

        private static void Update(float deltaTime)
        {
            //recordTime += deltaTime;
            //if (recordTime >= 0.2f)
            //{
                application.Window.Title = string.Concat("Test ", MathF.Round(1f / deltaTime).ToString());
                //recordTime -= 0.2f;
            //}
            //Here all updates to the program should be done.
        }
    }
}