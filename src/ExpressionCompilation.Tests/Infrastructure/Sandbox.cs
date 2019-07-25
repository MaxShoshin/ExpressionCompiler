using System;
using System.Diagnostics;
using System.Reflection;
using JetBrains.Annotations;

namespace NeedfulThings.ExpressionCompilation.Tests.Infrastructure
{
    internal abstract class Sandbox : MarshalByRefObject
    {
        [NotNull]
        public static IDisposable Create<T>()
            where T : Sandbox
        {
            return CreateAppDomainHost<T>();
        }

        protected abstract void Start();

        protected abstract void Stop();

        [NotNull]
        private static IDisposable CreateAppDomainHost<T>()
            where T : Sandbox
        {
            var domainName = GetAppDomainName<T>();

            var info = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
            };

            var domain = AppDomain.CreateDomain(domainName, null, info);
            T host = null;

            try
            {
                var assemblyName = typeof(T).Assembly.FullName;

                host = (T)domain.CreateInstanceAndUnwrap(
                    assemblyName,
                    typeof(T).FullName,
                    false,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                    null,
                    null,
                    null,
                    null);

                domain.UnhandledException += Domain_UnhandledException;
                host.Start();

                return new DomainUnloader(host, domain);
            }
            catch (Exception)
            {
                DomainUnloader.UnloadDomain(host, domain);
                throw;
            }
        }

        private static void Domain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
        }

        [NotNull]
        private static string GetAppDomainName<T>()
        {
            return typeof(T).Name;
        }

        private sealed class DomainUnloader : IDisposable
        {
            [NotNull]
            private readonly Sandbox _box;

            [NotNull]
            private readonly AppDomain _domain;

            public DomainUnloader([NotNull] Sandbox box, [NotNull] AppDomain domain)
            {
                _box = box;
                _domain = domain;
            }

            public static void UnloadDomain([CanBeNull] Sandbox box, [CanBeNull] AppDomain domain)
            {
                if (box != null)
                {
                    box.Stop();
                }

                if (domain == null)
                {
                    return;
                }

                const int lastAttempt = 3;

                for (int i = 0; i <= lastAttempt; i++)
                {
                    try
                    {
                        AppDomain.Unload(domain);
                        return;
                    }
                    catch (CannotUnloadAppDomainException ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                    catch (AppDomainUnloadedException ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }
            }

            public void Dispose()
            {
                UnloadDomain(_box, _domain);
            }
        }
    }
}