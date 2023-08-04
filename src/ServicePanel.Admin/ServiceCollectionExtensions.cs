using ServicePanel.Admin.Services;
using System.Runtime.InteropServices;

namespace ServicePanel.Admin;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServicePanel(this IServiceCollection services)
    {
        services.AddScoped<LayoutService>();
        services.AddSingleton<DefaultUiNotificationService>();
        services.AddSingleton<MachineHoldService>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<ServiceControlService>();
        }

        IFreeSql fsqlFactory(IServiceProvider r)
        {
            var logFactory = r.GetRequiredService<ILoggerFactory>();
            var logger = logFactory.CreateLogger("FreeSql.CommandText");
            var freeSql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.Sqlite, @"Data Source=ServicePanel.db")
#if DEBUG
                .UseMonitorCommand(cmd => logger.LogInformation("Sql：{CommandText}", cmd.CommandText))//监听SQL语句
#endif
                .UseAutoSyncStructure(true) //自动同步实体结构到数据库，FreeSql不会扫描程序集，只有CRUD时才会生成表。
                .Build();

            freeSql.CodeFirst.ConfigEntity<LabelEntity>(p =>
            {
                p.Property(x => x.Name).IsPrimary(true).IsNullable(false);
                p.Property(x => x.IP).IsPrimary(true).IsNullable(true);
            });

            freeSql.CodeFirst.ConfigEntity<UpdateEntity>(p =>
            {
                p.Property(x => x.Version).IsIdentity(true);
            });

            return freeSql;
        }

        services.AddSingleton(fsqlFactory);

        return services;
    }
}
