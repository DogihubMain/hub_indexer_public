using CommandLine;
using DogiHubIndexer;
using DogiHubIndexer.Providers.Interfaces;
using DogiHubIndexer.Providers;
using DogiHubIndexer.Services;
using DogiHubIndexer.Services.Interfaces;
using DogiHubIndexer.Validators;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using DogiHubIndexer.Repositories.RawData.Interfaces;
using DogiHubIndexer.Repositories.RawData;
using DogiHubIndexer.Repositories.ReadModels;
using DogiHubIndexer.Repositories.ReadModels.Interfaces;
using System.Globalization;

class Program
{
    static async Task Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        await Parser.Default
             .ParseArguments<Options>(args)
             .WithNotParsed(HandleParseError)
             .WithParsedAsync(RunOptions);
    }

    static void ConfigureServices(IServiceCollection services, Options options)
    {
        services.AddSingleton(Log.Logger);
        services.AddSingleton(options);

        services.AddScoped<IBlockRawDataRepository, BlockRawdataRepository>();
        services.AddScoped<IInscriptionRawDataRepository, InscriptionRawDataRepository>();
        services.AddScoped<IInscriptionTransferRawDataRepository, InscriptionTransferRawDataRepository>();
        services.AddScoped<IOutputRawDataRepository, OutputRawDataRepository>();

        services.AddScoped<ITokenInfoReadModelRepository, TokenInfoReadModelRedisRepository>();
        services.AddScoped<IUserBalanceTokensReadModelRepository, UserBalanceTokensReadModelRedisRepository>();
        services.AddScoped<IUserBalanceDnsReadModelRepository, UserBalanceDnsReadModelRedisRepository>();
        services.AddScoped<IUserBalanceDogemapsReadModelRepository, UserBalanceDogemapsReadModelRedisRepository>();
        services.AddScoped<IUserBalanceNftsReadModelRepository, UserBalanceNftsReadModelRedisRepository>();

        services.AddScoped<IBlockService, BlockService>();
        services.AddScoped<IDoginalsIndexerService, DoginalsIndexerService>();
        services.AddSingleton<IInscriptionService, InscriptionService>();
        services.AddScoped<IInscriptionTransferService, InscriptionTransferService>();
        services.AddScoped<ITransactionService, TransactionService>();

        services.AddSingleton<InscriptionValidator>();

        ConfigureRedis(services, options);
    }

    private static void ConfigureRedis(IServiceCollection services, Options options)
    {
        services.AddSingleton<IRedisClient, RedisClient>(sp =>
            new RedisClient(
                options.RedisConnectionString,
                options,
                Log.Logger,
                options.FlushRedis.GetValueOrDefault(false)
            )
        );
    }

    static async Task RunOptions(Options options)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                path: options.LogFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: null
            )
            .CreateLogger();

        Log.Logger.Information("indexer running");

        var services = new ServiceCollection();
        ConfigureServices(services, options);
        var serviceProvider = services.BuildServiceProvider();

        var doginalsIndexerService = serviceProvider.GetService<IDoginalsIndexerService>();
        await doginalsIndexerService!.RunAsync(options);
    }

    static void HandleParseError(IEnumerable<Error> errors)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Errors(s) when reading parameters, thanks to use --help to have all information :");
        foreach (var error in errors)
        {
            Console.WriteLine(error.Tag);
        }

        Console.ResetColor();
    }
}
