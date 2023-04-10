using NScenario;
using NUnit.Framework;
using RocksDb.Extensions.Tests.Utils;
using Shouldly;

namespace RocksDb.Extensions.Tests;

public class DeleteExistingDatabaseOnStartupTests
{
    [Test]
    public async Task should_delete_database_on_start_up()
    {
        var scenario = TestScenarioFactory.Default();
        var path = Path.Join(".", Guid.NewGuid().ToString("N"));

        await scenario.Step("Create a new RockDb database and initialize it with some values", () =>
        {
            using var testFixture = TestFixture.Create(rockDb =>
             {
                 _ = rockDb.AddStore<int, int, RocksDbGenericStore<int, int>>("my-store");
             }, options =>
            {
                options.Path = path;
            });

            var store = testFixture.GetStore<RocksDbGenericStore<int, int>>();
            store.Put(1, 1);
        });

        await scenario.Step("Re-create the database using the same path", async () =>
        {
            using var testFixture = TestFixture.Create(rockDb =>
            {
                _ = rockDb.AddStore<int, int, RocksDbGenericStore<int, int>>("my-store");
            }, options =>
            {
                options.Path = path;
                options.DeleteExistingDatabaseOnStartup = true;
            });

            await scenario.Step("Verify that the value is not available after the restart", () =>
            {
                var store = testFixture.GetStore<RocksDbGenericStore<int, int>>();
                store.TryGet(1, out _).ShouldBeFalse();
            });
        });
    }

    [Test]
    public async Task should_not_delete_database_on_start_up()
    {
        var scenario = TestScenarioFactory.Default();
        var path = Path.Join(".", Guid.NewGuid().ToString("N"));

        await scenario.Step("Create a new RockDb database and initialize it with some values", () =>
        {
            using var testFixture = TestFixture.Create(rockDb =>
            {
                _ = rockDb.AddStore<int, int, RocksDbGenericStore<int, int>>("my-store");
            }, options =>
            {
                options.Path = path;
            });

            var store = testFixture.GetStore<RocksDbGenericStore<int, int>>();
            store.Put(1, 1);
        });

        await scenario.Step("Re-create the database using the same path", async () =>
        {
            using var testFixture = TestFixture.Create(rockDb =>
            {
                _ = rockDb.AddStore<int, int, RocksDbGenericStore<int, int>>("my-store");
            }, options =>
            {
                options.Path = path;
                options.DeleteExistingDatabaseOnStartup = false;
            });

            await scenario.Step("Verify that the value is not available after the restart", () =>
            {
                var store = testFixture.GetStore<RocksDbGenericStore<int, int>>();
                store.TryGet(1, out var value).ShouldBeTrue();
                value.ShouldBe(1);
            });
        });
    }
}