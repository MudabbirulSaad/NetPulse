Console.WriteLine("Deployment Activity 1: Pass Task Completed!");
Console.WriteLine("Press any key to exit...");

if (!Console.IsInputRedirected)
{
    Console.ReadKey(intercept: true);
}
