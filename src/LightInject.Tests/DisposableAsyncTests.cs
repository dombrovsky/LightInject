using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LightInject.SampleLibrary;
using Xunit;

namespace LightInject.Tests
{
    public class DisposableAsyncTests
    {
        [Fact]
        public async Task DisposeAsync_AsyncDisposableServiceWithPerScopeLifetime_IsDisposed()
        {
            var container = CreateContainer();
            var disposableFoo = new AsyncDisposableFoo();
            container.Register<IFoo>(factory => disposableFoo, new PerScopeLifetime());
            await using (container.BeginScope())
            {
                container.GetInstance<IFoo>();
            }

            Assert.True(disposableFoo.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_DisposableServiceWithPerScopeLifetime_IsDisposed()
        {
            var container = CreateContainer();
            var disposableFoo = new DisposableFoo();
            container.Register<IFoo>(factory => disposableFoo, new PerScopeLifetime());
            await using (container.BeginScope())
            {
                container.GetInstance<IFoo>();
            }

            Assert.True(disposableFoo.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_AsyncDisposableServiceWithPerRequestLifetime_IsDisposed()
        {
            var container = CreateContainer();
            var disposableFoo = new AsyncDisposableFoo();
            container.Register<IFoo>(factory => disposableFoo, new PerRequestLifeTime());
            await using (container.BeginScope())
            {
                container.GetInstance<IFoo>();
            }

            Assert.True(disposableFoo.IsDisposed);
        }


        [Fact]
        public async Task DisposeAsync_DisposableServiceWithPerRequestLifetime_IsDisposed()
        {
            var container = CreateContainer();
            var disposableFoo = new DisposableFoo();
            container.Register<IFoo>(factory => disposableFoo, new PerRequestLifeTime());
            await using (container.BeginScope())
            {
                container.GetInstance<IFoo>();
            }

            Assert.True(disposableFoo.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_Singeltons_DisposesInReverseOrderOfCreation()
        {
            var container = CreateContainer();
            container.Register<FakeDisposeCallback>(new PerContainerLifetime());
            container.Register<ISingleton, Singleton1>("1", new PerContainerLifetime());
            container.Register<ISingleton, Singleton2>("2", new PerContainerLifetime());
            container.Register<ISingleton, Singleton3>("3", new PerContainerLifetime());
            container.Register<ISingleton, Singleton4>("4", new PerContainerLifetime());
            container.Register<ISingleton, Singleton5>("5", new PerContainerLifetime());

            var instances = container.GetAllInstances<ISingleton>();

            var disposableCallback = container.GetInstance<FakeDisposeCallback>();

            await container.DisposeAsync();

            Assert.IsType<Singleton5>(disposableCallback.Disposed[0]);
            Assert.IsType<Singleton4>(disposableCallback.Disposed[1]);
            Assert.IsType<Singleton3>(disposableCallback.Disposed[2]);
            Assert.IsType<Singleton2>(disposableCallback.Disposed[3]);
            Assert.IsType<Singleton1>(disposableCallback.Disposed[4]);
        }

        [Fact]
        public async Task DisposeAsync_Scoped_DisposesInReverseOrderOfCreation()
        {
            var container = CreateContainer();
            container.Register<FakeDisposeCallback>(new PerContainerLifetime());
            container.Register<ISingleton, Singleton1>("1", new PerScopeLifetime());
            container.Register<ISingleton, Singleton2>("2", new PerScopeLifetime());
            container.Register<ISingleton, Singleton3>("3", new PerScopeLifetime());
            container.Register<ISingleton, Singleton4>("4", new PerScopeLifetime());
            container.Register<ISingleton, Singleton5>("5", new PerScopeLifetime());
            await using (container.BeginScope())
            {
                var instances = container.GetAllInstances<ISingleton>();
            }

            var disposableCallback = container.GetInstance<FakeDisposeCallback>();

            await container.DisposeAsync();

            Assert.IsType<Singleton5>(disposableCallback.Disposed[0]);
            Assert.IsType<Singleton4>(disposableCallback.Disposed[1]);
            Assert.IsType<Singleton3>(disposableCallback.Disposed[2]);
            Assert.IsType<Singleton2>(disposableCallback.Disposed[3]);
            Assert.IsType<Singleton1>(disposableCallback.Disposed[4]);
        }

        [Fact]
        public async Task DisposeAsync_DisposesBothDisposableAndAsyncDisposableInstances()
        {
            var container = CreateContainer();
            var disposableFoo = new DisposableFoo();
            var asyncDisposableFoo = new AsyncDisposableFoo();
            container.Register<IFoo>(factory => disposableFoo, "1", new PerContainerLifetime());
            container.Register<IFoo>(factory => asyncDisposableFoo, "2", new PerContainerLifetime());
            
            var instances = container.GetAllInstances<IFoo>();

            await container.DisposeAsync();

            Assert.True(disposableFoo.IsDisposed);
            Assert.True(asyncDisposableFoo.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_Scope_CallsCompletedHandler()
        {
            var container = CreateContainer();
            bool wasCalled = false;
            await using (var scope = container.BeginScope())
            {
                scope.Completed += (s, a) => wasCalled = true;
            }

            Assert.True(wasCalled);
        }

        private static IServiceContainer CreateContainer()
        {
            return new ServiceContainer();
        }

        public class FakeDisposeCallback
        {
            public List<object> Disposed
            {
                get;
            } = new List<object>();
        }

        public interface ISingleton
        {

        }

        public class Singleton1 : IAsyncDisposable, ISingleton
        {
            private readonly FakeDisposeCallback fakeDisposeCallback;

            public Singleton1(FakeDisposeCallback fakeDisposeCallback)
            {
                this.fakeDisposeCallback = fakeDisposeCallback;
            }

            public async ValueTask DisposeAsync()
            {
                fakeDisposeCallback.Disposed.Add(this);
                await Task.FromResult(0);
            }
        }

        public class Singleton2 : Singleton1
        {
            public Singleton2(FakeDisposeCallback fakeDisposeCallback) : base(fakeDisposeCallback)
            {
            }
        }

        public class Singleton3 : Singleton1
        {
            public Singleton3(FakeDisposeCallback fakeDisposeCallback) : base(fakeDisposeCallback)
            {
            }
        }

        public class Singleton4 : Singleton1
        {
            public Singleton4(FakeDisposeCallback fakeDisposeCallback) : base(fakeDisposeCallback)
            {
            }
        }

        public class Singleton5 : Singleton1
        {
            public Singleton5(FakeDisposeCallback fakeDisposeCallback) : base(fakeDisposeCallback)
            {
            }
        }
    }
}