﻿using Audit.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Audit.EntityFramework.Edmx.UnitTest
{
    [TestFixture]
    public class UnitTest1
    {
        [SetUp]
        public void SetUp()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsEntities>().Reset();
        }

        [Test]
        public void Test_Delete_Ignored()
        {
            bool neverTrue = false;
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x.OnInsertAndReplace(ev =>
                {
                    neverTrue = true;
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(x => x.OnScopeCreated(sc =>
                {
                    var efEvent = sc.Event.GetEntityFrameworkEvent();
                }));

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsEntities>(config => config
                    .IncludeEntityObjects(false)
                    .AuditEventType("{context}:{database}"))
                .UseOptOut()
                .IgnoreAny(x =>
                {
                    Assert.AreEqual("Post", x.Name);
                    return x.Name == "Post";
                });

            using (var ctx = new BlogsEntities())
            {
                var post = new Post()
                {
                    DateCreated = DateTime.Now,
                    Content = "test-content-x",
                    BlogId = 1
                };
                ctx.Posts.Add(post);
                ctx.SaveChanges();
            }

            Assert.False(neverTrue);
        }

        [Test]
        public void TestMethod1()
        {
            var guid = Guid.NewGuid().ToString();
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsEntities>(x => x.
                    IncludeEntityObjects(true));
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }));
            using (var ctx = new BlogsEntities())
            {
                var post = new Post()
                {
                    Id = 1,
                    DateCreated = DateTime.Now,
                    Content = "test-content",
                    BlogId = 1
                };
                ctx.Posts.Add(post);
                ctx.SaveChanges();

                var postProxy = ctx.Posts.First();
                postProxy.Content = guid;
                ctx.SaveChanges();
            }

            Assert.AreEqual(2, evs.Count);

            Assert.AreEqual("Posts", evs[0].GetEntityFrameworkEvent().Entries[0].Table);
            Assert.AreEqual("Posts", evs[1].GetEntityFrameworkEvent().Entries[0].Table);

            var p1 = evs[0].GetEntityFrameworkEvent().Entries[0].Entity as Post;
            var p2 = evs[1].GetEntityFrameworkEvent().Entries[0].Entity as Post;

            Assert.AreEqual("test-content", p1.Content);
            Assert.AreEqual(guid, p2.Content);
        }
    }
}
