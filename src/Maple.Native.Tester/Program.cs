using Maple.Native;

Console.WriteLine($"Maple.Native version: {typeof(Native).Assembly.GetName().Version}");
Native.Empty();
Console.WriteLine("OK");
