using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EfGraph
{
    class Program
    {
        static void Main(string[] args)
        {
            var masterClient = GetMasterClient();
            var localClient = GetLocalClient();

            var masterOptions = new DbContextOptionsBuilder<TestContext>()
                .UseInMemoryDatabase("master")
                .Options;

            var localOptions = new DbContextOptionsBuilder<TestContext>()
                .UseInMemoryDatabase("local")
                .Options;

            using (var masterCtx = new TestContext(masterOptions))
            using (var localCtx = new TestContext(localOptions))
            {
                masterCtx.Add(masterClient.ToEntity());
                masterCtx.SaveChanges();
                localCtx.Add(localClient.ToEntity());
                localCtx.SaveChanges();

                Sync(masterCtx, localCtx, masterClient.ClientId);
            }
        }

        private static void Sync(TestContext masterDb, TestContext localDb, string clientId)
        {
            var masterClient = masterDb.Clients.AsNoTracking().IncludeAll().FirstOrDefault(c => c.ClientId == "test");
            var localClient = localDb.Clients.IncludeAll().FirstOrDefault(c => c.ClientId == "test");

            var localEntry = localDb.Entry(localClient);
            foreach (var collection in localEntry.Collections)
            {
            }
        }

        private static void MergeCollections<T>(ICollection<T> source, ICollection<T> target, Func<T, T, bool> comparer, TestContext targetContext)
            where T : class
        {
            foreach (var item in source)
            {
                var targetEntry = targetContext.Attach(item);

                var targetItem = target.FirstOrDefault(x => comparer(x, item));
                if (targetItem == null)
                {
                    targetEntry.State = EntityState.Deleted;
                }
                else
                {
                    targetEntry.CurrentValues.SetValues(item);
                    targetEntry.State = EntityState.Modified;
                }
            }

            foreach (var item in target)
            {
                var existsInSource = source.Any(x => comparer(x, item));
                if (!existsInSource)
                {
                    var targetEntry = targetContext.Attach(item);
                    targetEntry.State = EntityState.Added;
                }
            }
        }

        private static Client GetMasterClient()
        {
            var client = new Client()
            {
                ClientId = "test"
            };

            client.AllowedCorsOrigins = new List<string>()
            {
                "http://localhost:3001"
            };

            client.AllowedGrantTypes = new List<string>()
            {
                "client_credentials"
            };

            client.AllowedScopes = new List<string>()
            {
                "scope1", "scope2", "scope3"
            };

            return client;
        }

        private static Client GetLocalClient()
        {
            var client = new Client()
            {
                ClientId = "test"
            };

            client.AllowedCorsOrigins = new List<string>()
            {
                "http://localhost:3000"
            };

            client.AllowedGrantTypes = new List<string>()
            {
                "client_credentials", "hybrid"
            };

            client.AllowedScopes = new List<string>()
            {
                "scope1", "scope2"
            };

            return client;
        }
    }
}
