namespace BrainfuckToIL.Cli;

internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Gets a required service from a <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="services">The services to resolve from.</param>
    /// <typeparam name="T">The type of the required service to get.</typeparam>
    public static T GetRequiredService<T>(this IServiceProvider services) =>
        (T)(services.GetService(typeof(T)) ?? throw new InvalidOperationException(
            $"No service of type {typeof(T).FullName} has been registered."));
}
