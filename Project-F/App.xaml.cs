namespace Project_F
{
    public partial class App : Application
    {
        // GLOBAL USER ID
        public static string UserId { get; set; }

        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}